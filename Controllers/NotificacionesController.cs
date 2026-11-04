using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;

namespace UniLink.Controllers;

[Authorize]
public class NotificacionesController(UniLinkDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Forbid();
        }

        var notificaciones = await db.Notificaciones
            .AsNoTracking()
            .Where(notificacion => notificacion.UsuarioId == usuarioId.Value)
            .OrderByDescending(notificacion => notificacion.Fecha)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Notificaciones";
        ViewData["NoLeidas"] = notificaciones.Count(notificacion => !notificacion.Leida);
        return View(notificaciones);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarTodas(CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Forbid();
        }

        var pendientes = await db.Notificaciones
            .Where(notificacion => notificacion.UsuarioId == usuarioId.Value && !notificacion.Leida)
            .ToListAsync(cancellationToken);

        foreach (var notificacion in pendientes)
        {
            notificacion.Leida = true;
        }

        await db.SaveChangesAsync(cancellationToken);
        TempData["Mensaje"] = "Todas las notificaciones se marcaron como leídas.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Abrir(int id, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Forbid();
        }

        var notificacion = await db.Notificaciones
            .FirstOrDefaultAsync(
                item => item.Id == id && item.UsuarioId == usuarioId.Value,
                cancellationToken);
        if (notificacion is null)
        {
            return NotFound();
        }

        if (!notificacion.Leida)
        {
            notificacion.Leida = true;
            await db.SaveChangesAsync(cancellationToken);
        }

        return !string.IsNullOrWhiteSpace(notificacion.Url) && Url.IsLocalUrl(notificacion.Url)
            ? LocalRedirect(notificacion.Url)
            : RedirectToAction(nameof(Index));
    }

    private int? ObtenerUsuarioId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var usuarioId) ? usuarioId : null;
    }
}
