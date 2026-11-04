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
    private readonly RecomendacionService _recomendacionService;

    public PanelController(
        ICategoriaService categoriaService,
        UniLinkDbContext db,
        RecomendacionService recomendacionService)
    {
        _categoriaService = categoriaService;
        _db = db;
        _recomendacionService = recomendacionService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var usuarioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        var nombre = HttpContext.Session.GetString("NombreUsuario") ?? User.Identity?.Name ?? "estudiante";
        ViewData["NombreUsuario"] = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "estudiante";
        ViewData["AlquileresActivos"] = usuarioId is null
            ? 0
            : await _db.Alquileres.CountAsync(
                alquiler => alquiler.SolicitanteId == usuarioId.Value && alquiler.Estado == EstadoSolicitud.Activo,
                cancellationToken);
        ViewData["ClasesProximas"] = usuarioId is null
            ? 0
            : await _db.ReservasClase.CountAsync(
                reserva => reserva.FechaHora >= DateTime.UtcNow
                    && reserva.Estado == EstadoSolicitud.Aprobado
                    && (reserva.AlumnoId == usuarioId.Value || reserva.Clase.TutorId == usuarioId.Value),
                cancellationToken);
        ViewData["SolicitudesPendientes"] = usuarioId is null
            ? 0
            : await _db.Alquileres.CountAsync(
                alquiler => alquiler.Material.PropietarioId == usuarioId.Value && alquiler.Estado == EstadoSolicitud.Pendiente,
                cancellationToken);

        var categorias = await _categoriaService.ObtenerCategoriasAsync(cancellationToken);
        ViewData["Recomendaciones"] = usuarioId is null
            ? Array.Empty<RecomendacionViewModel>()
            : await _recomendacionService.ObtenerAsync(usuarioId.Value, cancellationToken);
        return View(categorias);
    }
}
