using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;
using UniLink.Services;

namespace UniLink.Controllers;

[ApiController]
[Authorize]
[Route("api/correos")]
public sealed class CorreosController(
    UniLinkDbContext db,
    ColaCorreos colaCorreos,
    ILogger<CorreosController> logger) : ControllerBase
{
    /// <summary>Publica un correo de prueba en la cola de correos de UniLink.</summary>
    /// <returns>Confirmación de que el correo fue publicado en RabbitMQ.</returns>
    [HttpGet("prueba")]
    [EndpointSummary("Encola un correo de prueba")]
    [EndpointDescription("Publica un correo de prueba para la cuenta autenticada. En modo demo Resend:CorreoDemo recibe el mensaje.")]
    public async Task<IActionResult> Prueba(CancellationToken cancellationToken)
    {
        var idUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idUsuario, out var usuarioId))
        {
            return Unauthorized();
        }

        var correo = await db.Usuarios.AsNoTracking()
            .Where(usuario => usuario.Id == usuarioId)
            .Select(usuario => usuario.Email)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(correo))
        {
            return BadRequest(new { error = "La cuenta no tiene un correo electrónico configurado." });
        }

        var publicado = await colaCorreos.PublicarAsync(
            new CorreoMensaje(
                correo,
                "Correo de prueba de UniLink",
                "prueba",
                new Dictionary<string, string>
                {
                    ["Titulo"] = "La cola de correos funciona",
                    ["Mensaje"] = "Este correo confirma que la aplicación publicó un mensaje en CloudAMQP.",
                    ["BotonTexto"] = "Explorar materiales",
                    ["BotonUrl"] = Url.Action(
                        "Index",
                        "Materiales",
                        values: null,
                        protocol: Request.Scheme) ?? string.Empty
                }),
            cancellationToken);

        if (!publicado)
        {
            logger.LogWarning("No se pudo publicar el correo de prueba solicitado por el usuario {UsuarioId}.", usuarioId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "La cola de correos no está disponible o no está configurada."
            });
        }

        return Ok(new { mensaje = "Correo de prueba publicado en la cola." });
    }
}
