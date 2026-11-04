using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using UniLink.ML;

namespace UniLink.Controllers;

[ApiController]
[Route("api/ml")]
public class SentimientoController(
    PredictionEnginePool<DatosSentimiento, PrediccionSentimiento> predicciones) : ControllerBase
{
    [HttpGet("sentimiento")]
    public IActionResult Predecir([FromQuery] string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return BadRequest(new { error = "El parámetro texto es obligatorio." });
        }

        if (texto.Length > 500)
        {
            return BadRequest(new { error = "El texto no puede superar los 500 caracteres." });
        }

        var prediccion = predicciones.Predict(new DatosSentimiento { Texto = texto.Trim() });
        return Ok(new
        {
            sentimiento = prediccion.Positivo ? "Positivo" : "Negativo",
            probabilidad = prediccion.Confianza
        });
    }
}
