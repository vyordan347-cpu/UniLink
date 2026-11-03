using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;

namespace UniLink.Controllers;

[Authorize]
public class AlquileresController(
    UniLinkDbContext db,
    ILogger<AlquileresController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Solicitar(int id, CancellationToken cancellationToken)
    {
        var material = await db.Materiales
            .AsNoTracking()
            .Include(item => item.Categoria)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (material is null)
        {
            return NotFound();
        }

        if (material.PropietarioId == ObtenerUsuarioId())
        {
            return Forbid();
        }

        if (!material.Disponible)
        {
            TempData["Error"] = "Este material no está disponible para solicitar.";
            return RedirectToAction("Detalle", "Materiales", new { id });
        }

        var fechaInicio = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        ViewData["Title"] = "Solicitar alquiler";
        return View(new SolicitudAlquilerViewModel
        {
            MaterialId = material.Id,
            FechaInicio = fechaInicio,
            FechaFin = fechaInicio,
            Material = material
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(
        SolicitudAlquilerViewModel modelo,
        CancellationToken cancellationToken)
    {
        var material = await db.Materiales
            .Include(item => item.Categoria)
            .FirstOrDefaultAsync(item => item.Id == modelo.MaterialId, cancellationToken);

        if (material is null)
        {
            return NotFound();
        }

        var solicitanteId = ObtenerUsuarioId();
        if (solicitanteId is null)
        {
            return Forbid();
        }

        if (material.PropietarioId == solicitanteId.Value)
        {
            return Forbid();
        }

        if (!modelo.AceptaCondiciones)
        {
            ModelState.AddModelError(nameof(modelo.AceptaCondiciones), "Debes aceptar las condiciones del préstamo.");
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        if (modelo.FechaInicio < hoy)
        {
            ModelState.AddModelError(nameof(modelo.FechaInicio), "La fecha de inicio no puede ser pasada.");
        }

        if (modelo.FechaFin < modelo.FechaInicio)
        {
            ModelState.AddModelError(nameof(modelo.FechaFin), "La fecha de devolución debe ser igual o posterior al inicio.");
        }

        if (!material.Disponible)
        {
            ModelState.AddModelError(string.Empty, "Este material ya no está disponible.");
        }

        var hayCruceConfirmado = await db.Alquileres.AnyAsync(alquiler =>
            alquiler.MaterialId == material.Id
            && (alquiler.Estado == EstadoSolicitud.Aprobado || alquiler.Estado == EstadoSolicitud.Activo)
            && alquiler.FechaInicio <= modelo.FechaFin
            && alquiler.FechaFin >= modelo.FechaInicio,
            cancellationToken);
        if (hayCruceConfirmado)
        {
            ModelState.AddModelError(string.Empty, "El material ya tiene un alquiler aprobado o activo en esas fechas.");
        }

        var costoTotal = 0m;
        if (modelo.FechaFin >= modelo.FechaInicio)
        {
            var cantidadDias = modelo.FechaFin.DayNumber - modelo.FechaInicio.DayNumber + 1;
            costoTotal = cantidadDias * material.PrecioDia;
            if (costoTotal > 99999999.99m)
            {
                ModelState.AddModelError(string.Empty, "El costo total supera el máximo permitido.");
            }
        }

        if (!ModelState.IsValid)
        {
            foreach (var (campo, estado) in ModelState)
            {
                foreach (var error in estado.Errors)
                {
                    var mensaje = string.IsNullOrEmpty(error.ErrorMessage)
                        ? error.Exception?.Message ?? "Error de validación sin mensaje."
                        : error.ErrorMessage;
                    logger.LogWarning("Solicitud de alquiler no válida. Campo: {Campo}. Error: {Mensaje}", campo, mensaje);
                }
            }

            modelo.Material = material;
            ViewData["Title"] = "Solicitar alquiler";
            return View(modelo);
        }

        var nombreSolicitante = await db.Usuarios
            .Where(usuario => usuario.Id == solicitanteId.Value)
            .Select(usuario => usuario.Nombre)
            .FirstAsync(cancellationToken);
        var alquiler = new Alquiler
        {
            MaterialId = material.Id,
            SolicitanteId = solicitanteId.Value,
            FechaInicio = modelo.FechaInicio,
            FechaFin = modelo.FechaFin,
            CostoTotal = costoTotal,
            Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = DateTime.UtcNow
        };

        db.Alquileres.Add(alquiler);
        db.Notificaciones.Add(new Notificacion
        {
            UsuarioId = material.PropietarioId,
            Mensaje = $"Recibiste una solicitud de {nombreSolicitante} para {material.Nombre}."
        });
        await db.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Confirmacion), new { id = alquiler.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmacion(int id, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Forbid();
        }

        var alquiler = await db.Alquileres
            .AsNoTracking()
            .Include(item => item.Material)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (alquiler is null)
        {
            return NotFound();
        }

        if (alquiler.SolicitanteId != usuarioId.Value)
        {
            return Forbid();
        }

        ViewData["Title"] = "Solicitud enviada";
        return View(alquiler);
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Forbid();
        }

        var alquileres = await db.Alquileres
            .AsNoTracking()
            .Include(alquiler => alquiler.Material)
            .Where(alquiler => alquiler.SolicitanteId == usuarioId.Value)
            .OrderByDescending(alquiler => alquiler.FechaSolicitud)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Mis alquileres";
        return View(alquileres);
    }

    [HttpGet]
    public async Task<IActionResult> Recibidas(CancellationToken cancellationToken)
    {
        var propietarioId = ObtenerUsuarioId();
        if (propietarioId is null)
        {
            return Forbid();
        }

        var alquileres = await db.Alquileres
            .AsNoTracking()
            .Include(alquiler => alquiler.Material)
            .Include(alquiler => alquiler.Solicitante)
            .Where(alquiler => alquiler.Material.PropietarioId == propietarioId.Value)
            .OrderByDescending(alquiler => alquiler.FechaSolicitud)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Solicitudes recibidas";
        return View(alquileres);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id, CancellationToken cancellationToken)
    {
        var alquiler = await ObtenerAlquilerParaActualizarAsync(id, cancellationToken);
        if (alquiler is null)
        {
            return NotFound();
        }

        if (!EsPropietario(alquiler))
        {
            return Forbid();
        }

        if (alquiler.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "Solo puedes aprobar solicitudes pendientes.";
            return RedirectToAction(nameof(Recibidas));
        }

        var solicitudesEnConflicto = await db.Alquileres
            .Include(item => item.Solicitante)
            .Include(item => item.Material)
            .Where(item => item.MaterialId == alquiler.MaterialId
                && item.Id != alquiler.Id
                && item.Estado == EstadoSolicitud.Pendiente
                && item.FechaInicio <= alquiler.FechaFin
                && item.FechaFin >= alquiler.FechaInicio)
            .ToListAsync(cancellationToken);

        alquiler.Estado = EstadoSolicitud.Aprobado;
        foreach (var conflicto in solicitudesEnConflicto)
        {
            conflicto.Estado = EstadoSolicitud.Rechazado;
            db.Notificaciones.Add(CrearNotificacion(
                conflicto.SolicitanteId,
                $"Tu solicitud de {conflicto.Material.Nombre} fue rechazada porque otro alquiler se aprobó para esas fechas."));
        }

        db.Notificaciones.Add(CrearNotificacion(
            alquiler.SolicitanteId,
            $"Tu solicitud de {alquiler.Material.Nombre} fue aprobada."));
        await db.SaveChangesAsync(cancellationToken);

        TempData["Mensaje"] = "Solicitud aprobada. Las solicitudes pendientes que se cruzaban fueron rechazadas.";
        return RedirectToAction(nameof(Recibidas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, CancellationToken cancellationToken)
    {
        var alquiler = await ObtenerAlquilerParaActualizarAsync(id, cancellationToken);
        if (alquiler is null)
        {
            return NotFound();
        }

        if (!EsPropietario(alquiler))
        {
            return Forbid();
        }

        if (alquiler.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "Solo puedes rechazar solicitudes pendientes.";
            return RedirectToAction(nameof(Recibidas));
        }

        alquiler.Estado = EstadoSolicitud.Rechazado;
        db.Notificaciones.Add(CrearNotificacion(
            alquiler.SolicitanteId,
            $"Tu solicitud de {alquiler.Material.Nombre} fue rechazada."));
        await db.SaveChangesAsync(cancellationToken);

        TempData["Mensaje"] = "Solicitud rechazada.";
        return RedirectToAction(nameof(Recibidas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarEntregado(int id, CancellationToken cancellationToken)
    {
        var alquiler = await ObtenerAlquilerParaActualizarAsync(id, cancellationToken);
        if (alquiler is null)
        {
            return NotFound();
        }

        if (!EsPropietario(alquiler))
        {
            return Forbid();
        }

        if (alquiler.Estado != EstadoSolicitud.Aprobado)
        {
            TempData["Error"] = "Solo puedes marcar como entregado un alquiler aprobado.";
            return RedirectToAction(nameof(Recibidas));
        }

        alquiler.Estado = EstadoSolicitud.Activo;
        alquiler.Material.Disponible = false;
        db.Notificaciones.Add(CrearNotificacion(
            alquiler.SolicitanteId,
            $"El material {alquiler.Material.Nombre} fue marcado como entregado."));
        await db.SaveChangesAsync(cancellationToken);

        TempData["Mensaje"] = "Alquiler marcado como entregado.";
        return RedirectToAction(nameof(Recibidas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarDevuelto(int id, CancellationToken cancellationToken)
    {
        var alquiler = await ObtenerAlquilerParaActualizarAsync(id, cancellationToken);
        if (alquiler is null)
        {
            return NotFound();
        }

        if (!EsPropietario(alquiler))
        {
            return Forbid();
        }

        if (alquiler.Estado != EstadoSolicitud.Activo)
        {
            TempData["Error"] = "Solo puedes marcar como devuelto un alquiler activo.";
            return RedirectToAction(nameof(Recibidas));
        }

        alquiler.Estado = EstadoSolicitud.Devuelto;
        alquiler.Material.Disponible = true;
        db.Notificaciones.Add(CrearNotificacion(
            alquiler.SolicitanteId,
            $"El material {alquiler.Material.Nombre} fue marcado como devuelto."));
        await db.SaveChangesAsync(cancellationToken);

        TempData["Mensaje"] = "Alquiler marcado como devuelto.";
        return RedirectToAction(nameof(Recibidas));
    }

    private async Task<Alquiler?> ObtenerAlquilerParaActualizarAsync(int id, CancellationToken cancellationToken) =>
        await db.Alquileres
            .Include(alquiler => alquiler.Material)
            .FirstOrDefaultAsync(alquiler => alquiler.Id == id, cancellationToken);

    private int? ObtenerUsuarioId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var usuarioId) ? usuarioId : null;
    }

    private bool EsPropietario(Alquiler alquiler) =>
        ObtenerUsuarioId() is int usuarioId && alquiler.Material.PropietarioId == usuarioId;

    private static Notificacion CrearNotificacion(int usuarioId, string mensaje) =>
        new()
        {
            UsuarioId = usuarioId,
            Mensaje = mensaje
        };
}
