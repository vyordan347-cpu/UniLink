using System.ComponentModel;
using Microsoft.SemanticKernel;
using UniLink.Services;

namespace UniLink.Agente.Plugins;

[Description("Obtiene recomendaciones de materiales y clases para el estudiante autenticado.")]
public sealed class RecomendacionesPlugin(RecomendacionService recomendacionService, int usuarioId)
{
    [KernelFunction("RecomendarParaMi")]
    [Description("Muestra hasta cinco materiales o clases recomendados para el estudiante autenticado.")]
    public async Task<string> RecomendarParaMi(
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        var recomendaciones = await recomendacionService.ObtenerAsync(usuarioId, cancellationToken);
        var resultados = recomendaciones.Take(5).ToList();
        return resultados.Count == 0
            ? "Aún no tengo recomendaciones para ti. Puedes explorar materiales y clases disponibles."
            : string.Join(Environment.NewLine, resultados.Select(item =>
                $"- {item.Tipo}: {item.Titulo} · {item.Carrera} · " +
                (item.EsSolidario ? "gratuito" : $"S/ {item.Precio:0.00} {item.UnidadPrecio}") +
                $" · [{item.UrlDetalle}]({item.UrlDetalle})"));
    }
}
