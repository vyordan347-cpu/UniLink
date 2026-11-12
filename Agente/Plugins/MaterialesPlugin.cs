using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using UniLink.Data;
using UniLink.Helpers;

namespace UniLink.Agente.Plugins;

[Description("Consulta materiales publicados y disponibles para alquiler en UniLink.")]
public sealed class MaterialesPlugin(
    UniLinkDbContext db,
    int usuarioId,
    ILogger<MaterialesPlugin> logger)
{
    [KernelFunction("BuscarMateriales")]
    [Description("Busca hasta cinco materiales disponibles que coincidan con los filtros y devuelve enlaces a sus detalles.")]
    public async Task<string> BuscarMateriales(
        [Description("Texto a buscar parcialmente en nombre, descripción, categoría o carrera.")] string? texto = null,
        [Description("Carrera relacionada con el material, por ejemplo Odontología o Medicina.")] string? carrera = null,
        [Description("Precio máximo por día en soles; omítelo si no hay límite.")] decimal? precioMaximo = null,
        [Description("Indica si solo se deben buscar materiales gratuitos o solidarios.")] bool soloGratuitos = false,
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        texto = texto?.Trim();
        carrera = carrera?.Trim();

        if (precioMaximo.HasValue)
        {
            if (precioMaximo.Value < 0)
            {
                logger.LogInformation(
                    "BuscarMateriales recibió del LLM texto={Texto}, carrera={Carrera}, precioMaximo={PrecioMaximo}, soloGratuitos={SoloGratuitos}; resultados=0 por precio inválido.",
                    texto,
                    carrera,
                    precioMaximo,
                    soloGratuitos);
                return "El precio máximo debe ser cero o mayor.";
            }
        }

        var materiales = await BuscarAsync(texto, carrera, precioMaximo, soloGratuitos, cancellationToken);
        if (materiales.Count == 0
            && !string.IsNullOrWhiteSpace(texto)
            && (!string.IsNullOrWhiteSpace(carrera) || precioMaximo.HasValue || soloGratuitos))
        {
            materiales = await BuscarAsync(texto, null, null, false, cancellationToken);
        }

        logger.LogInformation(
            "BuscarMateriales recibió del LLM texto={Texto}, carrera={Carrera}, precioMaximo={PrecioMaximo}, soloGratuitos={SoloGratuitos}; resultados={CantidadResultados}.",
            texto,
            carrera,
            precioMaximo,
            soloGratuitos,
            materiales.Count);

        return materiales.Count == 0
            ? "No encontré materiales disponibles con esos filtros."
            : string.Join(Environment.NewLine, materiales.Select(material =>
                $"- {material.Nombre} · {material.Carrera} · " +
                (material.EsSolidario ? "gratuito" : $"S/ {material.PrecioDia:0.00} por día") +
                $" · [/Materiales/Detalle/{material.Id}](/Materiales/Detalle/{material.Id})"));
    }

    private IQueryable<MaterialResultado> CrearConsulta(
        string? texto,
        string? carrera,
        decimal? precioMaximo,
        bool soloGratuitos)
    {
        var consulta = db.Materiales.AsNoTracking()
            .Where(material => material.Disponible && material.PropietarioId != usuarioId);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var patron = $"%{TextoBusqueda.NormalizarSinTildes(texto)}%";
            consulta = consulta.Where(material =>
                EF.Functions.ILike(UniLinkDbContext.Unaccent(material.Nombre), patron)
                || EF.Functions.ILike(UniLinkDbContext.Unaccent(material.Descripcion), patron)
                || EF.Functions.ILike(UniLinkDbContext.Unaccent(material.Categoria.Nombre), patron)
                || EF.Functions.ILike(UniLinkDbContext.Unaccent(material.Carrera), patron));
        }

        if (!string.IsNullOrWhiteSpace(carrera))
        {
            var patronCarrera = $"%{TextoBusqueda.NormalizarSinTildes(carrera)}%";
            consulta = consulta.Where(material =>
                EF.Functions.ILike(UniLinkDbContext.Unaccent(material.Carrera), patronCarrera));
        }

        if (precioMaximo.HasValue)
        {
            consulta = consulta.Where(material => material.PrecioDia <= precioMaximo.Value);
        }

        if (soloGratuitos)
        {
            consulta = consulta.Where(material => material.EsSolidario);
        }

        return consulta
            .OrderByDescending(material => material.FechaPublicacion)
            .Take(5)
            .Select(material => new MaterialResultado(
                material.Id,
                material.Nombre,
                material.Carrera,
                material.PrecioDia,
                material.EsSolidario));
    }

    private async Task<List<MaterialResultado>> BuscarAsync(
        string? texto,
        string? carrera,
        decimal? precioMaximo,
        bool soloGratuitos,
        CancellationToken cancellationToken) =>
        await CrearConsulta(texto, carrera, precioMaximo, soloGratuitos)
            .ToListAsync(cancellationToken);

    private sealed record MaterialResultado(
        int Id,
        string Nombre,
        string Carrera,
        decimal PrecioDia,
        bool EsSolidario);

    [KernelFunction("ObtenerDetalleMaterial")]
    [Description("Obtiene el resumen de un material y un enlace relativo a su ficha.")]
    public async Task<string> ObtenerDetalleMaterial(
        [Description("Identificador numérico del material.")] int id,
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        var material = await db.Materiales.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.Nombre,
                item.Descripcion,
                item.Carrera,
                item.Ubicacion,
                item.PrecioDia,
                item.EsSolidario,
                item.Disponible,
                Categoria = item.Categoria.Nombre
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (material is null)
        {
            return "No encontré ese material.";
        }

        var precio = material.EsSolidario ? "gratuito" : $"S/ {material.PrecioDia:0.00} por día";
        var disponibilidad = material.Disponible ? "disponible" : "no disponible actualmente";
        return $"{material.Nombre} ({material.Categoria}, {material.Carrera}). " +
            $"{material.Descripcion[..Math.Min(material.Descripcion.Length, 300)]} " +
            $"Ubicación: {material.Ubicacion}. Precio: {precio}. " +
            $"Estado: {disponibilidad}. [/Materiales/Detalle/{material.Id}](/Materiales/Detalle/{material.Id})";
    }
}
