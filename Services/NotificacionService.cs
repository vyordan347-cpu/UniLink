using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using UniLink.Data;
using UniLink.Hubs;
using UniLink.Models;

namespace UniLink.Services;

public sealed class NotificacionService(
    UniLinkDbContext db,
    IHubContext<NotificacionesHub> hubContext)
{
    public async Task CrearYEnviarAsync(
        int usuarioId,
        string mensaje,
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(usuarioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var notificacion = new Notificacion
        {
            UsuarioId = usuarioId,
            Mensaje = mensaje,
            Url = url,
            Fecha = DateTime.UtcNow
        };

        db.Notificaciones.Add(notificacion);
        await db.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .User(usuarioId.ToString(CultureInfo.InvariantCulture))
            .SendAsync(
                "RecibirNotificacion",
                new
                {
                    id = notificacion.Id,
                    mensaje = notificacion.Mensaje,
                    url = notificacion.Url,
                    fecha = notificacion.Fecha
                },
                cancellationToken);
    }
}
