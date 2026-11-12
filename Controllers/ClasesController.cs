using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Helpers;
using UniLink.Models;
using UniLink.Services;

namespace UniLink.Controllers;

public class ClasesController(
    UniLinkDbContext db,
    BusquedaService busquedaService,
    ColaCorreos colaCorreos,
    NotificacionService notificacionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? curso,
        string? carrera,
        bool soloGratuitas,
        CancellationToken cancellationToken)
    {
        curso = NormalizarTexto(curso, 150);
        carrera = NormalizarTexto(carrera, 100);
        var consulta = CrearConsultaExplorar(curso, carrera, soloGratuitas);
        var modelo = new ClasesExplorarViewModel
        {
            Clases = await consulta.OrderByDescending(clase => clase.FechaPublicacion)
                .ToListAsync(cancellationToken),
            Carreras = await db.Clases.AsNoTracking()
                .Select(clase => clase.Tutor.Carrera)
                .Distinct()
                .OrderBy(nombre => nombre)
                .ToListAsync(cancellationToken),
            Curso = curso,
            Carrera = carrera,
            SoloGratuitas = soloGratuitas
        };

        ViewData["Title"] = "Explorar clases";
        return View(modelo);
    }

    [HttpGet("/api/clases")]
    public async Task<IActionResult> Api(
        string? curso,
        string? carrera,
        bool soloGratuitas,
        CancellationToken cancellationToken)
    {
        curso = NormalizarTexto(curso, 150);
        carrera = NormalizarTexto(carrera, 100);
        var clases = await CrearConsultaExplorar(curso, carrera, soloGratuitas)
            .OrderByDescending(clase => clase.FechaPublicacion)
            .Select(clase => new
            {
                clase.Id,
                clase.Curso,
                clase.Descripcion,
                modalidad = ModalidadClaseHelper.ObtenerEtiqueta(clase.Modalidad),
                clase.PrecioHora,
                clase.EsSolidaria,
                tutor = clase.Tutor.Nombre,
                carreraTutor = clase.Tutor.Carrera
            })
            .ToListAsync(cancellationToken);

        return Ok(clases);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        var clase = await db.Clases.AsNoTracking()
            .Include(item => item.Tutor)
            .Include(item => item.Resenas)
                .ThenInclude(resena => resena.Autor)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (clase is null)
        {
            return NotFound();
        }

        ViewData["EsTutor"] = EsTutor(clase);
        ViewData["ResumenResenas"] = new ResenasResumenViewModel
        {
            Resenas = clase.Resenas.OrderByDescending(resena => resena.Fecha).ToList(),
            PromedioCalificacion = clase.Resenas.Count == 0 ? 0 : clase.Resenas.Average(resena => resena.Calificacion)
        };
        ViewData["Title"] = clase.Curso;
        return View(clase);
    }

    [Authorize]
    [HttpGet]
    public IActionResult Ofrecer()
    {
        ViewData["Title"] = "Ofrecer clase";
        return View(new ClaseViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ofrecer(ClaseViewModel modelo, CancellationToken cancellationToken)
    {
        ValidarOferta(modelo);
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Ofrecer clase";
            return View(modelo);
        }

        var tutorId = ObtenerUsuarioId();
        if (tutorId is null)
        {
            return Forbid();
        }

        var clase = new Clase
        {
            Curso = modelo.Curso.Trim(),
            Descripcion = modelo.Descripcion.Trim(),
            Modalidad = modelo.Modalidad,
            PrecioHora = modelo.EsSolidaria ? 0 : modelo.PrecioHora,
            EsSolidaria = modelo.EsSolidaria,
            TutorId = tutorId.Value,
            FechaPublicacion = DateTime.UtcNow
        };

        db.Clases.Add(clase);
        await db.SaveChangesAsync(cancellationToken);
        await busquedaService.SincronizarClaseAsync(clase.Id, cancellationToken);
        TempData["Mensaje"] = "¡Clase publicada!";
        return RedirectToAction(nameof(Detalle), new { id = clase.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
    {
        var clase = await db.Clases.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (clase is null)
        {
            return NotFound();
        }

        if (!EsTutor(clase))
        {
            return Forbid();
        }

        ViewData["Title"] = "Editar clase";
        return View(ConvertirViewModel(clase));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ClaseViewModel modelo, CancellationToken cancellationToken)
    {
        var clase = await db.Clases.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (clase is null)
        {
            return NotFound();
        }

        if (!EsTutor(clase))
        {
            return Forbid();
        }

        modelo.Id = id;
        ValidarOferta(modelo);
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar clase";
            return View(modelo);
        }

        clase.Curso = modelo.Curso.Trim();
        clase.Descripcion = modelo.Descripcion.Trim();
        clase.Modalidad = modelo.Modalidad;
        clase.PrecioHora = modelo.EsSolidaria ? 0 : modelo.PrecioHora;
        clase.EsSolidaria = modelo.EsSolidaria;
        await db.SaveChangesAsync(cancellationToken);
        await busquedaService.SincronizarClaseAsync(clase.Id, cancellationToken);

        TempData["Mensaje"] = "Clase actualizada correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var clase = await db.Clases.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (clase is null)
        {
            return NotFound();
        }

        if (!EsTutor(clase))
        {
            return Forbid();
        }

        if (await db.ReservasClase.AnyAsync(reserva => reserva.ClaseId == id, cancellationToken))
        {
            TempData["Error"] = "No se puede eliminar una clase que tiene reservas registradas.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        db.Clases.Remove(clase);
        await db.SaveChangesAsync(cancellationToken);
        await busquedaService.SincronizarClaseAsync(id, cancellationToken);
        TempData["Mensaje"] = "Clase eliminada correctamente.";
        return RedirectToAction(nameof(Mias), new { pestaña = "tutor" });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Reservar(int id, CancellationToken cancellationToken)
    {
        var clase = await db.Clases.AsNoTracking()
            .Include(item => item.Tutor)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (clase is null)
        {
            return NotFound();
        }

        if (EsTutor(clase))
        {
            return Forbid();
        }

        ViewData["Title"] = "Reservar clase";
        return View(new ReservaClaseViewModel { ClaseId = id, Clase = clase });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(ReservaClaseViewModel modelo, CancellationToken cancellationToken)
    {
        var clase = await db.Clases.AsNoTracking()
            .Include(item => item.Tutor)
            .FirstOrDefaultAsync(item => item.Id == modelo.ClaseId, cancellationToken);
        if (clase is null)
        {
            return NotFound();
        }

        var alumnoId = ObtenerUsuarioId();
        if (alumnoId is null)
        {
            return Forbid();
        }

        if (clase.TutorId == alumnoId.Value)
        {
            return Forbid();
        }

        if (!modelo.AceptaCondiciones)
        {
            ModelState.AddModelError(nameof(modelo.AceptaCondiciones), "Debes aceptar las condiciones para reservar.");
        }

        if (modelo.DesfaseHorarioMinutos is < -840 or > 840)
        {
            ModelState.AddModelError(nameof(modelo.FechaHora), "No se pudo validar la zona horaria del dispositivo.");
        }

        var fechaUtc = DateTime.SpecifyKind(
            modelo.FechaHora.AddMinutes(modelo.DesfaseHorarioMinutos),
            DateTimeKind.Utc);
        if (fechaUtc <= DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(modelo.FechaHora), "La fecha y hora no pueden ser pasadas.");
        }

        var costoTotal = modelo.DuracionHoras is >= 1 and <= 3
            ? modelo.DuracionHoras * clase.PrecioHora
            : 0m;
        if (costoTotal > 99999999.99m)
        {
            ModelState.AddModelError(string.Empty, "El costo total supera el máximo permitido.");
        }

        if (!ModelState.IsValid)
        {
            modelo.Clase = clase;
            ViewData["Title"] = "Reservar clase";
            return View(modelo);
        }

        var reserva = new ReservaClase
        {
            ClaseId = clase.Id,
            AlumnoId = alumnoId.Value,
            FechaHora = fechaUtc,
            DuracionHoras = modelo.DuracionHoras,
            CostoTotal = costoTotal,
            TemaAReforzar = string.IsNullOrWhiteSpace(modelo.TemaAReforzar)
                ? null
                : modelo.TemaAReforzar.Trim(),
            Estado = EstadoSolicitud.Pendiente
        };
        var nombreAlumno = await db.Usuarios
            .Where(usuario => usuario.Id == alumnoId.Value)
            .Select(usuario => usuario.Nombre)
            .FirstAsync(cancellationToken);

        db.ReservasClase.Add(reserva);
        await db.SaveChangesAsync(cancellationToken);
        await notificacionService.CrearYEnviarAsync(
            clase.TutorId,
            $"Recibiste una solicitud de {nombreAlumno} para una clase de {clase.Curso}.",
            Url.Action(nameof(Mias), "Clases", new { pestaña = "tutor" })
                ?? throw new InvalidOperationException("No se pudo generar la URL de las clases del tutor."),
            cancellationToken);

        return RedirectToAction(nameof(Confirmacion), new { id = reserva.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Confirmacion(int id, CancellationToken cancellationToken)
    {
        var alumnoId = ObtenerUsuarioId();
        if (alumnoId is null)
        {
            return Forbid();
        }

        var reserva = await db.ReservasClase.AsNoTracking()
            .Include(item => item.Clase)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (reserva.AlumnoId != alumnoId.Value)
        {
            return Forbid();
        }

        ViewData["Title"] = "Reserva enviada";
        return View(reserva);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mias(string? pestaña, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Forbid();
        }

        var reservasAlumno = await db.ReservasClase.AsNoTracking()
            .Include(reserva => reserva.Clase)
                .ThenInclude(clase => clase.Tutor)
            .Include(reserva => reserva.Resena)
            .Where(reserva => reserva.AlumnoId == usuarioId.Value)
            .OrderBy(reserva => reserva.FechaHora)
            .ToListAsync(cancellationToken);
        var reservasTutor = await db.ReservasClase.AsNoTracking()
            .Include(reserva => reserva.Clase)
                .ThenInclude(clase => clase.Tutor)
            .Include(reserva => reserva.Resena)
            .Include(reserva => reserva.Alumno)
            .Where(reserva => reserva.Clase.TutorId == usuarioId.Value)
            .OrderBy(reserva => reserva.FechaHora)
            .ToListAsync(cancellationToken);
        var clasesTutor = await db.Clases.AsNoTracking()
            .Where(clase => clase.TutorId == usuarioId.Value)
            .OrderByDescending(clase => clase.FechaPublicacion)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Mis clases";
        return View(new MisClasesViewModel
        {
            Pestaña = string.Equals(pestaña, "tutor", StringComparison.OrdinalIgnoreCase) ? "tutor" : "alumno",
            ReservasComoAlumno = reservasAlumno,
            ReservasComoTutor = reservasTutor,
            ClasesComoTutor = clasesTutor
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id, string? enlaceReunion, CancellationToken cancellationToken)
    {
        var reserva = await ObtenerReservaAsync(id, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!EsTutor(reserva.Clase))
        {
            return Forbid();
        }

        if (reserva.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "Solo puedes aprobar reservas pendientes.";
            return RedirectToAction(nameof(Mias), new { pestaña = "tutor" });
        }

        enlaceReunion = enlaceReunion?.Trim();
        if (!string.IsNullOrEmpty(enlaceReunion) && !EsUrlHttpValida(enlaceReunion))
        {
            TempData["Error"] = "El enlace personalizado debe ser una URL válida con http o https.";
            return RedirectToAction(nameof(Mias), new { pestaña = "tutor" });
        }

        var conflictos = await db.ReservasClase
            .Include(item => item.Clase)
            .Where(item => item.ClaseId == reserva.ClaseId
                && item.Id != reserva.Id
                && item.Estado == EstadoSolicitud.Pendiente
                && item.FechaHora == reserva.FechaHora)
            .ToListAsync(cancellationToken);

        reserva.Estado = EstadoSolicitud.Aprobado;
        reserva.EnlaceReunion = string.IsNullOrEmpty(enlaceReunion)
            ? CrearEnlaceJitsi(reserva.Clase.Curso)
            : enlaceReunion;
        foreach (var conflicto in conflictos)
        {
            conflicto.Estado = EstadoSolicitud.Rechazado;
        }

        await db.SaveChangesAsync(cancellationToken);
        var urlReservasAlumno = Url.Action(nameof(Mias), "Clases", new { pestaña = "alumno" })
            ?? throw new InvalidOperationException("No se pudo generar la URL de las reservas del alumno.");
        foreach (var conflicto in conflictos)
        {
            await notificacionService.CrearYEnviarAsync(
                conflicto.AlumnoId,
                $"Tu reserva de {conflicto.Clase.Curso} fue rechazada porque otro alumno reservó esa fecha y hora.",
                urlReservasAlumno,
                cancellationToken);
        }

        await notificacionService.CrearYEnviarAsync(
            reserva.AlumnoId,
            $"Tu reserva para {reserva.Clase.Curso} fue aprobada.",
            urlReservasAlumno,
            cancellationToken);
        await colaCorreos.PublicarAsync(
            new CorreoMensaje(
                reserva.Alumno.Email ?? string.Empty,
                "Tu reserva de clase fue confirmada",
                "reserva-clase-confirmada",
                new Dictionary<string, string>
                {
                    ["Titulo"] = "Reserva de clase confirmada",
                    ["Mensaje"] = $"Tu reserva para {reserva.Clase.Curso} fue aprobada.",
                    ["BotonTexto"] = "Ver clase",
                    ["BotonUrl"] = Url.Action(
                        "Detalle",
                        "Clases",
                        new { id = reserva.ClaseId },
                        Request.Scheme) ?? string.Empty,
                    ["EnlaceReunion"] = reserva.EnlaceReunion ?? string.Empty
                }),
            cancellationToken);

        TempData["Mensaje"] = "Reserva aprobada. Las otras solicitudes pendientes para esa fecha y hora fueron rechazadas.";
        return RedirectToAction(nameof(Mias), new { pestaña = "tutor" });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, CancellationToken cancellationToken)
    {
        var reserva = await ObtenerReservaAsync(id, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!EsTutor(reserva.Clase))
        {
            return Forbid();
        }

        if (reserva.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "Solo puedes rechazar reservas pendientes.";
            return RedirectToAction(nameof(Mias), new { pestaña = "tutor" });
        }

        reserva.Estado = EstadoSolicitud.Rechazado;
        await db.SaveChangesAsync(cancellationToken);
        await notificacionService.CrearYEnviarAsync(
            reserva.AlumnoId,
            $"Tu reserva para {reserva.Clase.Curso} fue rechazada.",
            Url.Action(nameof(Mias), "Clases", new { pestaña = "alumno" })
                ?? throw new InvalidOperationException("No se pudo generar la URL de las reservas del alumno."),
            cancellationToken);

        TempData["Mensaje"] = "Reserva rechazada.";
        return RedirectToAction(nameof(Mias), new { pestaña = "tutor" });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarDictada(int id, CancellationToken cancellationToken)
    {
        var reserva = await ObtenerReservaAsync(id, cancellationToken);
        if (reserva is null)
        {
            return NotFound();
        }

        if (!EsTutor(reserva.Clase))
        {
            return Forbid();
        }

        if (reserva.Estado != EstadoSolicitud.Aprobado)
        {
            TempData["Error"] = "Solo puedes marcar como dictada una reserva aprobada.";
            return RedirectToAction(nameof(Mias), new { pestaña = "tutor" });
        }

        reserva.Estado = EstadoSolicitud.Devuelto;
        await db.SaveChangesAsync(cancellationToken);
        await notificacionService.CrearYEnviarAsync(
            reserva.AlumnoId,
            $"La clase de {reserva.Clase.Curso} fue marcada como dictada.",
            Url.Action(nameof(Mias), "Clases", new { pestaña = "alumno" })
                ?? throw new InvalidOperationException("No se pudo generar la URL de las reservas del alumno."),
            cancellationToken);

        TempData["Mensaje"] = "Clase marcada como dictada.";
        return RedirectToAction(nameof(Mias), new { pestaña = "tutor" });
    }

    private IQueryable<Clase> CrearConsultaExplorar(string? curso, string? carrera, bool soloGratuitas)
    {
        var consulta = db.Clases.AsNoTracking()
            .Include(clase => clase.Tutor)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(curso))
        {
            var patron = $"%{TextoBusqueda.NormalizarSinTildes(curso)}%";
            consulta = consulta.Where(clase =>
                EF.Functions.ILike(UniLinkDbContext.Unaccent(clase.Curso), patron));
        }

        if (!string.IsNullOrWhiteSpace(carrera))
        {
            var carreraNormalizada = TextoBusqueda.NormalizarSinTildes(carrera);
            consulta = consulta.Where(clase =>
                UniLinkDbContext.Unaccent(clase.Tutor.Carrera).ToLower() == carreraNormalizada);
        }

        if (soloGratuitas)
        {
            consulta = consulta.Where(clase => clase.EsSolidaria);
        }

        return consulta;
    }

    private async Task<ReservaClase?> ObtenerReservaAsync(int id, CancellationToken cancellationToken) =>
        await db.ReservasClase
            .Include(reserva => reserva.Clase)
            .Include(reserva => reserva.Alumno)
            .FirstOrDefaultAsync(reserva => reserva.Id == id, cancellationToken);

    private void ValidarOferta(ClaseViewModel modelo)
    {
        if (modelo.EsSolidaria)
        {
            modelo.PrecioHora = 0;
            ModelState.Remove(nameof(modelo.PrecioHora));
        }
        else if (modelo.PrecioHora <= 0 || modelo.PrecioHora > 99999999.99m)
        {
            ModelState.AddModelError(nameof(modelo.PrecioHora), "El precio por hora debe ser mayor que 0 y no superar 99 999 999,99 soles.");
        }

        if (!Enum.IsDefined(modelo.Modalidad))
        {
            ModelState.AddModelError(nameof(modelo.Modalidad), "Selecciona una modalidad válida.");
        }
    }

    private static bool EsUrlHttpValida(string url) =>
        url.Length <= 500
        && Uri.TryCreate(url, UriKind.Absolute, out var enlace)
        && (enlace.Scheme == Uri.UriSchemeHttp || enlace.Scheme == Uri.UriSchemeHttps);

    private static string CrearEnlaceJitsi(string curso)
    {
        var cursoSinEspacios = string.Concat(curso.Where(char.IsLetterOrDigit));
        if (string.IsNullOrEmpty(cursoSinEspacios))
        {
            cursoSinEspacios = "Clase";
        }

        var identificador = Guid.NewGuid().ToString("N")[..8];
        return $"https://meet.jit.si/UniLink-{cursoSinEspacios}-{identificador}";
    }

    private int? ObtenerUsuarioId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var usuarioId) ? usuarioId : null;
    }

    private bool EsTutor(Clase clase) =>
        ObtenerUsuarioId() is int usuarioId && clase.TutorId == usuarioId;

    private static string? NormalizarTexto(string? texto, int maximo)
    {
        var normalizado = texto?.Trim();
        if (string.IsNullOrEmpty(normalizado))
        {
            return null;
        }

        return normalizado.Length > maximo ? normalizado[..maximo] : normalizado;
    }

    private static ClaseViewModel ConvertirViewModel(Clase clase) =>
        new()
        {
            Id = clase.Id,
            Curso = clase.Curso,
            Descripcion = clase.Descripcion,
            Modalidad = clase.Modalidad,
            PrecioHora = clase.PrecioHora,
            EsSolidaria = clase.EsSolidaria
        };
}
