using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace UniLink.Services;

public sealed class UsuarioIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var id = connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var usuarioId)
            ? usuarioId.ToString(CultureInfo.InvariantCulture)
            : null;
    }
}
