using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using UniLink.Data;
using UniLink.ML;
using UniLink.Models;
using UniLink.Services;

namespace UniLink.Controllers;

[Authorize]
public class ResenasController(
    UniLinkDbContext db,
    PredictionEnginePool<DatosSentimiento, PrediccionSentimiento> predicciones,
    NotificacionService notificacionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Alquiler(int id, CancellationToken cancellationToken)
    {
        var alquiler = await ObtenerAlquilerAsync(id, cancellationToken);
        if (alquiler is null)
        {
            return NotFound();
        }

        if (!PuedeResenarAlquiler(alquiler))
        {
            return Forbid();
        }

        if (await db.Resenas.AnyAsync(resena => resena.AlquilerId == id, cancellationToken))
        {
            TempData["Error"] = "Ya dejaste una reseña para este alquiler.";
            return RedirectToAction("Index", "Alquileres");
        }

        ViewData["TituloOperacion"] = alquiler.Material.Nombre;
        ViewData["Title"] = "Dejar reseña";
        return View("Crear", new ResenaCrearViewModel { OperacionId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Alquiler(
        ResenaCrearViewModel modelo,
        CancellationToken cancellationToken)
    {
        var alquiler = await ObtenerAlquilerAsync(modelo.OperacionId, cancellationToken);
        if (alquiler is null)
        {
            return NotFound();
        }

        if (!PuedeResenarAlquiler(alquiler))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewData["TituloOperacion"] = alquiler.Material.Nombre;
            ViewData["Title"] = "Dejar reseña";
            return View("Crear", modelo);
        }

        await CrearResenaAsync(
            modelo,
            alquiler.SolicitanteId,
            alquiler.Material.PropietarioId,
            alquiler.MaterialId,
            null,
            alquiler.Id,
            null,
            cancellationToken);
        TempData["Mensaje"] = "Gracias por compartir tu experiencia.";
        await notificacionService.CrearYEnviarAsync(
            alquiler.Material.PropietarioId,
            $"Recibiste una nueva reseña sobre {alquiler.Material.Nombre}.",
            Url.Action("Perfil", "Cuenta", new { id = alquiler.Material.PropietarioId })
                ?? throw new InvalidOperationException("No se pudo generar la URL del perfil."),
            cancellationToken);
        return RedirectToAction("Index", "Alquileres");
    }

    [HttpGet]
    public async Task<IActionResult> Clase(int id, CancellationToken cancellationToken)
    {
        var reserva = await ObtenerReservaAsync(id, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!PuedeResenarClase(reserva))
        {
            return Forbid();
        }

        if (await db.Resenas.AnyAsync(resena => resena.ReservaClaseId == id, cancellationToken))
        {
            TempData["Error"] = "Ya dejaste una reseña para esta reserva.";
            return RedirectToAction("Mias", "Clases", new { pestaña = "alumno" });
        }

        ViewData["TituloOperacion"] = reserva.Clase.Curso;
        ViewData["Title"] = "Dejar reseña";
        return View("Crear", new ResenaCrearViewModel { OperacionId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clase(
        ResenaCrearViewModel modelo,
        CancellationToken cancellationToken)
    {
        var reserva = await ObtenerReservaAsync(modelo.OperacionId, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!PuedeResenarClase(reserva))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewData["TituloOperacion"] = reserva.Clase.Curso;
            ViewData["Title"] = "Dejar reseña";
            return View("Crear", modelo);
        }

        await CrearResenaAsync(
            modelo,
            reserva.AlumnoId,
            reserva.Clase.TutorId,
            null,
            reserva.ClaseId,
            null,
            reserva.Id,
            cancellationToken);
        TempData["Mensaje"] = "Gracias por compartir tu experiencia.";
        await notificacionService.CrearYEnviarAsync(
            reserva.Clase.TutorId,
            $"Recibiste una nueva reseña sobre la clase de {reserva.Clase.Curso}.",
            Url.Action("Perfil", "Cuenta", new { id = reserva.Clase.TutorId })
                ?? throw new InvalidOperationException("No se pudo generar la URL del perfil."),
            cancellationToken);
        return RedirectToAction("Mias", "Clases", new { pestaña = "alumno" });
    }

    private async Task CrearResenaAsync(
        ResenaCrearViewModel modelo,
        int autorId,
        int usuarioResenadoId,
        int? materialId,
        int? claseId,
        int? alquilerId,
        int? reservaClaseId,
        CancellationToken cancellationToken)
    {
        var prediccion = predicciones.Predict(new DatosSentimiento { Texto = modelo.Comentario.Trim() });
        var resena = new Resena
        {
            AutorId = autorId,
            UsuarioReseñadoId = usuarioResenadoId,
            MaterialId = materialId,
            ClaseId = claseId,
            AlquilerId = alquilerId,
            ReservaClaseId = reservaClaseId,
            Calificacion = modelo.Calificacion,
            Comentario = modelo.Comentario.Trim(),
            Sentimiento = prediccion.Positivo ? "Positivo" : "Negativo",
            Probabilidad = prediccion.Confianza,
            Fecha = DateTime.UtcNow
        };

        db.Resenas.Add(resena);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Alquiler?> ObtenerAlquilerAsync(int id, CancellationToken cancellationToken) =>
        await db.Alquileres
            .Include(alquiler => alquiler.Material)
            .FirstOrDefaultAsync(alquiler => alquiler.Id == id, cancellationToken);

    private async Task<ReservaClase?> ObtenerReservaAsync(int id, CancellationToken cancellationToken) =>
        await db.ReservasClase
            .Include(reserva => reserva.Clase)
            .FirstOrDefaultAsync(reserva => reserva.Id == id, cancellationToken);

    private bool PuedeResenarAlquiler(Alquiler alquiler) =>
        ObtenerUsuarioId() == alquiler.SolicitanteId
        && alquiler.Estado == EstadoSolicitud.Devuelto;

    private bool PuedeResenarClase(ReservaClase reserva) =>
        ObtenerUsuarioId() == reserva.AlumnoId
        && reserva.Estado == EstadoSolicitud.Devuelto;

    private int? ObtenerUsuarioId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
