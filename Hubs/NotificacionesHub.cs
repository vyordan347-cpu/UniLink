using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace UniLink.Hubs;

[Authorize]
public sealed class NotificacionesHub : Hub
{
}
