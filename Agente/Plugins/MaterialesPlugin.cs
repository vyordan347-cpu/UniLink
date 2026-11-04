using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using UniLink.Data;

namespace UniLink.Agente.Plugins;

[Description("Consulta materiales publicados y disponibles para alquiler en UniLink.")]
public sealed class MaterialesPlugin(UniLinkDbContext db, int usuarioId)
{
    [KernelFunction("BuscarMateriales")]
    [Description("Busca hasta cinco materiales disponibles que coincidan con los filtros y devuelve enlaces a sus detalles.")]
    public async Task<string> BuscarMateriales(
        [Description("Palabras del nombre o la descripción del material que se desea encontrar.")] string? texto = null,
        [Description("Carrera relacionada con el material, por ejemplo Odontología o Medicina.")] string? carrera = null,
        [Description("Precio máximo por día en soles; omítelo si no hay límite.")] decimal? precioMaximo = null,
        [Description("Indica si solo se deben buscar materiales gratuitos o solidarios.")] bool soloGratuitos = false,
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        var consulta = db.Materiales.AsNoTracking()
            .Where(material => material.Disponible && material.PropietarioId != usuarioId);

        texto = texto?.Trim();
        carrera = carrera?.Trim();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var textoNormalizado = texto.ToLowerInvariant();
            consulta = consulta.Where(material =>
                material.Nombre.ToLower().Contains(textoNormalizado)
                || material.Descripcion.ToLower().Contains(textoNormalizado)
                || material.Categoria.Nombre.ToLower().Contains(textoNormalizado));
        }

        if (!string.IsNullOrWhiteSpace(carrera))
        {
            var carreraNormalizada = carrera.ToLowerInvariant();
            consulta = consulta.Where(material => material.Carrera.ToLower().Contains(carreraNormalizada));
        }

        if (precioMaximo.HasValue)
        {
            if (precioMaximo.Value < 0)
            {
                return "El precio máximo debe ser cero o mayor.";
            }

            consulta = consulta.Where(material => material.PrecioDia <= precioMaximo.Value);
        }

        if (soloGratuitos)
        {
            consulta = consulta.Where(material => material.EsSolidario);
        }

        var materiales = await consulta
            .OrderByDescending(material => material.FechaPublicacion)
            .Take(5)
            .Select(material => new
            {
                material.Id,
                material.Nombre,
                material.Carrera,
                material.PrecioDia,
                material.EsSolidario
            })
            .ToListAsync(cancellationToken);

        return materiales.Count == 0
            ? "No encontré materiales disponibles con esos filtros."
            : string.Join(Environment.NewLine, materiales.Select(material =>
                $"- {material.Nombre} · {material.Carrera} · " +
                (material.EsSolidario ? "gratuito" : $"S/ {material.PrecioDia:0.00} por día") +
                $" · [/Materiales/Detalle/{material.Id}](/Materiales/Detalle/{material.Id})"));
    }

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
