using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;
using UniLink.Services;

namespace UniLink.Controllers;

[Authorize]
public class PanelController : Controller
{
    private readonly ICategoriaService _categoriaService;
    private readonly UniLinkDbContext _db;

    public PanelController(ICategoriaService categoriaService, UniLinkDbContext db)
    {
        _categoriaService = categoriaService;
        _db = db;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["NombreUsuario"] = HttpContext.Session.GetString("NombreUsuario") ?? User.Identity?.Name;
        var usuarioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        ViewData["AlquileresActivos"] = usuarioId is null
            ? 0
            : await _db.Alquileres.CountAsync(
                alquiler => alquiler.SolicitanteId == usuarioId.Value && alquiler.Estado == EstadoSolicitud.Activo,
                cancellationToken);
        ViewData["ClasesProximas"] = 0;
        ViewData["SolicitudesPendientes"] = usuarioId is null
            ? 0
            : await _db.Alquileres.CountAsync(
                alquiler => alquiler.Material.PropietarioId == usuarioId.Value && alquiler.Estado == EstadoSolicitud.Pendiente,
                cancellationToken);

        var categorias = await _categoriaService.ObtenerCategoriasAsync(cancellationToken);
        return View(categorias);
    }
}
