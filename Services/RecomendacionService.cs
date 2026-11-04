using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using UniLink.Data;
using UniLink.ML;
using UniLink.Models;

namespace UniLink.Services;

public sealed class RecomendacionService(
    UniLinkDbContext db,
    IDistributedCache cache,
    ModeloRecomendacion modelo)
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
            return JsonSerializer.Deserialize<List<RecomendacionViewModel>>(valorCache)
                ?? throw new InvalidDataException("La caché de recomendaciones contiene datos no válidos.");
        }

        var usuario = await db.Usuarios.AsNoTracking()
            .Where(item => item.Id == usuarioId)
            .Select(item => new { item.Carrera })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No se encontró el usuario autenticado.");

        var materialesAlquilados = await db.Alquileres.AsNoTracking()
            .Where(alquiler => alquiler.SolicitanteId == usuarioId
                && alquiler.Estado != EstadoSolicitud.Rechazado)
            .Select(alquiler => alquiler.MaterialId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var clasesReservadas = await db.ReservasClase.AsNoTracking()
            .Where(reserva => reserva.AlumnoId == usuarioId
                && reserva.Estado != EstadoSolicitud.Rechazado)
            .Select(reserva => reserva.ClaseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var materiales = await db.Materiales.AsNoTracking()
            .Where(material => material.Disponible
                && material.PropietarioId != usuarioId
                && !materialesAlquilados.Contains(material.Id))
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
            .Where(clase => clase.TutorId != usuarioId && !clasesReservadas.Contains(clase.Id))
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
        var tieneHistorial = materialesAlquilados.Count > 0 || clasesReservadas.Count > 0;
        List<RecomendacionViewModel> recomendaciones;
        if (candidatos.Count == 0)
        {
            recomendaciones = [];
        }
        else if (!tieneHistorial)
        {
            recomendaciones = candidatos
                .Where(candidato => string.Equals(candidato.Carrera, usuario.Carrera, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(candidato => candidato.Popularidad)
                .ThenByDescending(candidato => candidato.FechaPublicacion)
                .Take(6)
                .Select(candidato => candidato.AVista())
                .ToList();
        }
        else
        {
            var puntuaciones = modelo.Predecir(
                $"usuario_{usuarioId}",
                candidatos.Select(candidato => candidato.ClaveModelo).Distinct(StringComparer.Ordinal).ToArray());
            recomendaciones = candidatos
                .OrderByDescending(candidato => puntuaciones.TryGetValue(candidato.ClaveModelo, out var puntuacion)
                    ? puntuacion
                    : float.NegativeInfinity)
                .ThenByDescending(candidato => candidato.Popularidad)
                .Take(6)
                .Select(candidato => candidato.AVista())
                .ToList();
        }

        await cache.SetStringAsync(
            claveCache,
            JsonSerializer.Serialize(recomendaciones),
            OpcionesCache,
            cancellationToken);
        return recomendaciones;
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
