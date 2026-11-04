using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniLink.Models;
using UniLink.Services;

namespace UniLink.Controllers;

[ApiController]
[Authorize]
[Route("api/ml/recomendaciones")]
public sealed class RecomendacionesController(RecomendacionService recomendacionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecomendacionViewModel>>> Obtener(
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized();
        }

        var recomendaciones = await recomendacionService.ObtenerAsync(usuarioId, cancellationToken);
        return Ok(recomendaciones);
    }
}
