using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UniLink.Services;

namespace UniLink.Controllers;

[ApiController]
[Authorize]
[Route("api/asistente")]
public sealed class AsistenteController(
    AsistenteService asistenteService,
    ILogger<AsistenteController> logger) : ControllerBase
{
    [HttpPost("chat")]
    [EnableRateLimiting("asistente-chat")]
    public async Task<IActionResult> Chat(
        [FromBody] SolicitudChat solicitud,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized(new { respuesta = "Tu sesión ya no está activa. Inicia sesión nuevamente.", error = true });
        }

        try
        {
            var resultado = await asistenteService.ResponderAsync(
                usuarioId,
                solicitud.Mensaje.Trim(),
                cancellationToken);
            return Ok(new
            {
                respuesta = resultado.Respuesta,
                herramientasUsadas = resultado.HerramientasUsadas,
                proveedorUsado = resultado.ProveedorUsado,
                modeloUsado = resultado.ModeloUsado,
                error = false
            });
        }
        catch (AsistenteNoDisponibleException exception)
        {
            logger.LogWarning(exception, "El chat no pudo responder tras probar los modelos configurados.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                respuesta = "El asistente está muy solicitado en este momento. Intenta de nuevo en unos segundos 🙏",
                error = true
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ocurrió un error inesperado al procesar el chat.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                respuesta = "Ocurrió un problema al procesar tu mensaje. Inténtalo nuevamente.",
                error = true
            });
        }
    }

    [HttpPost("reiniciar")]
    public async Task<IActionResult> Reiniciar(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized(new { respuesta = "Tu sesión ya no está activa. Inicia sesión nuevamente.", error = true });
        }

        try
        {
            await asistenteService.ReiniciarAsync(usuarioId, cancellationToken);
            return Ok(new { mensaje = "Conversación reiniciada.", error = false });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo reiniciar la conversación del usuario.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                respuesta = "No se pudo reiniciar la conversación. Inténtalo nuevamente.",
                error = true
            });
        }
    }

    public sealed class SolicitudChat
    {
        [Required(ErrorMessage = "Escribe un mensaje para el asistente.")]
        [StringLength(2000, ErrorMessage = "El mensaje no puede superar los 2000 caracteres.")]
        public string Mensaje { get; set; } = string.Empty;
    }
}
