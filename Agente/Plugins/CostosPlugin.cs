using System.ComponentModel;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using UniLink.Data;
using UniLink.Helpers;

namespace UniLink.Agente.Plugins;

[Description("Calcula el costo informativo de un alquiler sin crear una solicitud.")]
public sealed class CostosPlugin(UniLinkDbContext db)
{
    [KernelFunction("CalcularCostoAlquiler")]
    [Description("Calcula el costo de alquiler contando el día inicial y el final; no crea solicitudes ni procesa pagos.")]
    public async Task<string> CalcularCostoAlquiler(
        [Description("Identificador numérico del material.")] int materialId,
        [Description("Fecha de inicio en formato AAAA-MM-DD.")] string fechaInicio,
        [Description("Fecha de fin en formato AAAA-MM-DD.")] string fechaFin,
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParse(fechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.None, out var inicio)
            || !DateOnly.TryParse(fechaFin, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fin))
        {
            return "Indícame las fechas en formato AAAA-MM-DD para calcular el costo.";
        }

        if (fin < inicio)
        {
            return "La fecha de fin debe ser igual o posterior a la fecha de inicio.";
        }

        var material = await db.Materiales.AsNoTracking()
            .Where(item => item.Id == materialId)
            .Select(item => new { item.Nombre, item.PrecioDia })
            .SingleOrDefaultAsync(cancellationToken);
        if (material is null)
        {
            return "No encontré ese material.";
        }

        var calculo = CalculadoraAlquiler.Calcular(inicio, fin, material.PrecioDia);
        return $"Alquiler de {material.Nombre}: {calculo.Dias} día(s) contando ambas fechas × " +
            $"S/ {material.PrecioDia:0.00} por día = S/ {calculo.CostoTotal:0.00}. " +
            "El pago se coordina en persona; UniLink no procesa pagos. " +
            $"[/Materiales/Detalle/{materialId}](/Materiales/Detalle/{materialId})";
    }
}
