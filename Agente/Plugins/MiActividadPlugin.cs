using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using UniLink.Data;
using UniLink.Models;

namespace UniLink.Agente.Plugins;

[Description("Consulta la actividad y las solicitudes del estudiante autenticado.")]
public sealed class MiActividadPlugin(UniLinkDbContext db, int usuarioId)
{
    [KernelFunction("MisAlquileresActivos")]
    [Description("Muestra hasta cinco alquileres propios aprobados o activos.")]
    public async Task<string> MisAlquileresActivos(
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        var alquileres = await db.Alquileres.AsNoTracking()
            .Where(alquiler => alquiler.SolicitanteId == usuarioId
                && (alquiler.Estado == EstadoSolicitud.Aprobado || alquiler.Estado == EstadoSolicitud.Activo))
            .OrderBy(alquiler => alquiler.FechaFin)
            .Take(5)
            .Select(alquiler => new
            {
                alquiler.MaterialId,
                alquiler.Material.Nombre,
                alquiler.FechaInicio,
                alquiler.FechaFin,
                alquiler.Estado
            })
            .ToListAsync(cancellationToken);

        return alquileres.Count == 0
            ? "No tienes alquileres aprobados o activos."
            : string.Join(Environment.NewLine, alquileres.Select(alquiler =>
                $"- {alquiler.Nombre} · {alquiler.Estado} · {alquiler.FechaInicio:dd/MM/yyyy}–{alquiler.FechaFin:dd/MM/yyyy} " +
                $"· [/Materiales/Detalle/{alquiler.MaterialId}](/Materiales/Detalle/{alquiler.MaterialId})"));
    }

    [KernelFunction("MisProximasClases")]
    [Description("Muestra hasta cinco próximas clases reservadas por el estudiante.")]
    public async Task<string> MisProximasClases(
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        var reservas = await db.ReservasClase.AsNoTracking()
            .Where(reserva => reserva.AlumnoId == usuarioId
                && reserva.FechaHora >= ahora
                && (reserva.Estado == EstadoSolicitud.Aprobado || reserva.Estado == EstadoSolicitud.Activo))
            .OrderBy(reserva => reserva.FechaHora)
            .Take(5)
            .Select(reserva => new
            {
                reserva.ClaseId,
                reserva.Clase.Curso,
                reserva.FechaHora,
                reserva.Estado
            })
            .ToListAsync(cancellationToken);

        return reservas.Count == 0
            ? "No tienes próximas clases aprobadas."
            : string.Join(Environment.NewLine, reservas.Select(reserva =>
                $"- {reserva.Curso} · {reserva.FechaHora.ToLocalTime():dd/MM/yyyy HH:mm} · {reserva.Estado} " +
                $"· [/Clases/Detalle/{reserva.ClaseId}](/Clases/Detalle/{reserva.ClaseId})"));
    }

    [KernelFunction("MisSolicitudesPendientes")]
    [Description("Muestra hasta cinco solicitudes pendientes propias o recibidas por el estudiante.")]
    public async Task<string> MisSolicitudesPendientes(
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        var alquileres = await db.Alquileres.AsNoTracking()
            .Where(alquiler => alquiler.Estado == EstadoSolicitud.Pendiente
                && (alquiler.SolicitanteId == usuarioId || alquiler.Material.PropietarioId == usuarioId))
            .OrderByDescending(alquiler => alquiler.FechaSolicitud)
            .Take(5)
            .Select(alquiler => new
            {
                alquiler.FechaSolicitud,
                IdDetalle = alquiler.MaterialId,
                Titulo = alquiler.Material.Nombre,
                Tipo = "Alquiler"
            })
            .ToListAsync(cancellationToken);

        var reservas = await db.ReservasClase.AsNoTracking()
            .Where(reserva => reserva.Estado == EstadoSolicitud.Pendiente
                && (reserva.AlumnoId == usuarioId || reserva.Clase.TutorId == usuarioId))
            .OrderByDescending(reserva => reserva.FechaHora)
            .Take(5)
            .Select(reserva => new
            {
                FechaSolicitud = reserva.FechaHora,
                IdDetalle = reserva.ClaseId,
                Titulo = reserva.Clase.Curso,
                Tipo = "Clase"
            })
            .ToListAsync(cancellationToken);

        var solicitudes = alquileres
            .Concat(reservas)
            .OrderByDescending(solicitud => solicitud.FechaSolicitud)
            .Take(5)
            .ToList();

        return solicitudes.Count == 0
            ? "No tienes solicitudes pendientes."
            : string.Join(Environment.NewLine, solicitudes.Select(solicitud =>
                $"- {solicitud.Tipo}: {solicitud.Titulo} · {solicitud.FechaSolicitud.ToLocalTime():dd/MM/yyyy} " +
                (solicitud.Tipo == "Alquiler"
                    ? $"· [/Materiales/Detalle/{solicitud.IdDetalle}](/Materiales/Detalle/{solicitud.IdDetalle})"
                    : $"· [/Clases/Detalle/{solicitud.IdDetalle}](/Clases/Detalle/{solicitud.IdDetalle})")));
    }
}
