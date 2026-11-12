using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using UniLink.Data;
using UniLink.Helpers;
using UniLink.ML;
using UniLink.Models;

namespace UniLink.Services;

public sealed class RecomendacionService(
    UniLinkDbContext db,
    IDistributedCache cache,
    ModeloRecomendacion modelo,
    ILogger<RecomendacionService> logger)
{
    private static readonly DistributedCacheEntryOptions OpcionesCache = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    public async Task<IReadOnlyList<RecomendacionViewModel>> ObtenerAsync(
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var claveCache = $"recomendaciones:usuario:{usuarioId}";
        var valorCache = await cache.GetStringAsync(claveCache, cancellationToken);
        if (valorCache is not null)
        {
            var recomendacionesEnCache = JsonSerializer.Deserialize<List<RecomendacionViewModel>>(valorCache)
                ?? throw new InvalidDataException("La caché de recomendaciones contiene datos no válidos.");
            if (recomendacionesEnCache.Count > 0)
            {
                return recomendacionesEnCache;
            }

            await cache.RemoveAsync(claveCache, cancellationToken);
        }

        var usuario = await db.Usuarios.AsNoTracking()
            .Where(item => item.Id == usuarioId)
            .Select(item => new { item.Carrera })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No se encontró el usuario autenticado.");

        var tieneAlquileres = await db.Alquileres.AsNoTracking()
            .AnyAsync(alquiler => alquiler.SolicitanteId == usuarioId
                && alquiler.Estado != EstadoSolicitud.Rechazado, cancellationToken);
        var tieneReservas = await db.ReservasClase.AsNoTracking()
            .AnyAsync(reserva => reserva.AlumnoId == usuarioId
                && reserva.Estado != EstadoSolicitud.Rechazado, cancellationToken);

        var materiales = await db.Materiales.AsNoTracking()
            .Where(material => material.Disponible
                && material.PropietarioId != usuarioId)
            .Select(material => new CandidatoRecomendacion(
                "Material",
                material.Id,
                material.Nombre,
                material.Descripcion,
                material.Carrera,
                material.PrecioDia,
                "por día",
                material.EsSolidario,
                material.ImagenUrl,
                $"/Materiales/Detalle/{material.Id}",
                $"material_categoria_{material.CategoriaId}",
                material.Alquileres.Count(alquiler => alquiler.Estado != EstadoSolicitud.Rechazado),
                material.FechaPublicacion))
            .ToListAsync(cancellationToken);
        var clasesDatos = await db.Clases.AsNoTracking()
            .Where(clase => clase.TutorId != usuarioId)
            .Select(clase => new
            {
                clase.Id,
                clase.Curso,
                clase.Descripcion,
                clase.Tutor.Carrera,
                clase.PrecioHora,
                clase.EsSolidaria,
                clase.FechaPublicacion,
                Popularidad = clase.Reservas.Count(reserva => reserva.Estado != EstadoSolicitud.Rechazado)
            })
            .ToListAsync(cancellationToken);
        var clases = clasesDatos.Select(clase => new CandidatoRecomendacion(
            "Clase",
            clase.Id,
            clase.Curso,
            clase.Descripcion,
            clase.Carrera,
            clase.PrecioHora,
            "por hora",
            clase.EsSolidaria,
            null,
            $"/Clases/Detalle/{clase.Id}",
            EntrenadorRecomendacion.ObtenerClaveClase(clase.Carrera),
            clase.Popularidad,
            clase.FechaPublicacion));

        var candidatos = materiales.Concat(clases).ToList();
        var tieneHistorial = tieneAlquileres || tieneReservas;
        var carreraUsuarioNormalizada = TextoBusqueda.NormalizarSinTildes(usuario.Carrera).Trim();
        List<RecomendacionViewModel> recomendaciones = [];
        string nivelUsado;
        if (candidatos.Count > 0 && tieneHistorial && modelo.PuedePredecir)
        {
            try
            {
                var puntuaciones = modelo.Predecir(
                    $"usuario_{usuarioId}",
                    candidatos.Select(candidato => candidato.ClaveModelo).Distinct(StringComparer.Ordinal).ToArray());
                if (puntuaciones.Count > 0)
                {
                    recomendaciones = candidatos
                        .OrderByDescending(candidato => puntuaciones.TryGetValue(candidato.ClaveModelo, out var puntuacion)
                            ? puntuacion
                            : float.NegativeInfinity)
                        .ThenByDescending(candidato => candidato.Popularidad)
                        .Take(6)
                        .Select(candidato => candidato.AVista())
                        .ToList();
                    nivelUsado = "modelo MatrixFactorization";
                    logger.LogInformation(
                        "Recomendaciones para usuario {UsuarioId}: se usó {Nivel}; {Cantidad} ítems devueltos.",
                        usuarioId,
                        nivelUsado,
                        recomendaciones.Count);
                    await GuardarCacheSiTieneResultadosAsync(claveCache, recomendaciones, cancellationToken);
                    return recomendaciones;
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "No se pudo usar MatrixFactorization para el usuario {UsuarioId}; se aplicará el fallback.",
                    usuarioId);
            }
        }

        var nivelFallback = "sin-resultados";
        if (AgregarCandidatos(
                candidatos
                    .Where(candidato => TextoBusqueda.NormalizarSinTildes(candidato.Carrera).Trim()
                        == carreraUsuarioNormalizada)
                    .OrderByDescending(candidato => candidato.Popularidad)
                    .ThenByDescending(candidato => candidato.FechaPublicacion),
                recomendaciones))
        {
            nivelFallback = "carrera";
        }

        if (recomendaciones.Count < 6
            && AgregarCandidatos(
                candidatos
                    .OrderByDescending(candidato => candidato.Popularidad)
                    .ThenByDescending(candidato => candidato.FechaPublicacion),
                recomendaciones))
        {
            nivelFallback = "plataforma";
        }

        if (recomendaciones.Count < 6
            && AgregarCandidatos(
                candidatos.OrderByDescending(candidato => candidato.FechaPublicacion),
                recomendaciones))
        {
            nivelFallback = "recientes";
        }

        nivelUsado = $"fallback {nivelFallback}";
        logger.LogInformation(
            "Recomendaciones para usuario {UsuarioId}: se usó {Nivel}; {Cantidad} ítems devueltos.",
            usuarioId,
            nivelUsado,
            recomendaciones.Count);

        await GuardarCacheSiTieneResultadosAsync(claveCache, recomendaciones, cancellationToken);
        return recomendaciones;
    }

    private async Task GuardarCacheSiTieneResultadosAsync(
        string claveCache,
        IReadOnlyList<RecomendacionViewModel> recomendaciones,
        CancellationToken cancellationToken)
    {
        if (recomendaciones.Count == 0)
        {
            return;
        }

        await cache.SetStringAsync(
            claveCache,
            JsonSerializer.Serialize(recomendaciones),
            OpcionesCache,
            cancellationToken);
    }

    private static bool AgregarCandidatos(
        IEnumerable<CandidatoRecomendacion> candidatos,
        List<RecomendacionViewModel> recomendaciones)
    {
        var idsAgregados = recomendaciones
            .Select(recomendacion => (recomendacion.Tipo, recomendacion.Id))
            .ToHashSet();
        var cantidadInicial = recomendaciones.Count;

        foreach (var candidato in candidatos)
        {
            if (recomendaciones.Count == 6)
            {
                break;
            }

            if (idsAgregados.Add((candidato.Tipo, candidato.Id)))
            {
                recomendaciones.Add(candidato.AVista());
            }
        }

        return recomendaciones.Count > cantidadInicial;
    }

    private sealed record CandidatoRecomendacion(
        string Tipo,
        int Id,
        string Titulo,
        string Descripcion,
        string Carrera,
        decimal Precio,
        string UnidadPrecio,
        bool EsSolidario,
        string? ImagenUrl,
        string UrlDetalle,
        string ClaveModelo,
        int Popularidad,
        DateTime FechaPublicacion)
    {
        public RecomendacionViewModel AVista() => new()
        {
            Tipo = Tipo,
            Id = Id,
            Titulo = Titulo,
            Descripcion = Descripcion,
            Carrera = Carrera,
            Precio = Precio,
            UnidadPrecio = UnidadPrecio,
            EsSolidario = EsSolidario,
            ImagenUrl = ImagenUrl,
            UrlDetalle = UrlDetalle
        };
    }
}
