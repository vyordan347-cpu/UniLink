# Notificaciones en tiempo real

- **Rama:** `feature/websocket`
- **Agente:** Copilot SDK en VS Code
- **Skill usada:** `diseno-unilink`

## Prompt

Lee AGENTS.md y usa la skill diseno-unilink. Estamos en la rama feature/websocket. Implementa notificaciones en tiempo real con SignalR:

1. Hubs/NotificacionesHub en /hubs/notificaciones con [Authorize]. Cada usuario se identifica por su Id (IUserIdProvider o Context.UserIdentifier).
2. Crea Services/NotificacionService con un método CrearYEnviarAsync(usuarioId, mensaje, url) que guarde la Notificacion en la base de datos y la envíe en tiempo real con IHubContext a ese usuario. Reemplaza en alquileres y clases todos los lugares donde hoy se crean notificaciones para usar este servicio.
3. En _Layout (solo con sesión): ícono de campana con contador de no leídas. Usa el cliente JavaScript de SignalR (desde CDN) para conectarse; al recibir una notificación, aumenta el contador y muestra un toast de Bootstrap con el mensaje y un enlace.
4. Página /Notificaciones ([Authorize]) con la lista (no leídas resaltadas), "Marcar todas como leídas" y clic en una notificación que la marque como leída y lleve a su url.
5. Si la conexión se cae, reconexión automática (withAutomaticReconnect).
6. Ejecuta dotnet build.
7. Crea docs/prompts/11-websocket.md con la rama, el agente, este prompt y un resumen.
8. NO ejecutes git.

## Resumen

Se agregó el hub autorizado de SignalR, que identifica cada conexión con el Id numérico de Identity, y el servicio que persiste cada notificación antes de enviarla al usuario correspondiente. Las solicitudes y cambios de estado de alquileres y reservas de clases ahora usan ese servicio y guardan enlaces internos para llegar a la actividad pertinente. Se añadió la bandeja protegida de notificaciones, el marcado de lectura, el contador en la navegación con sesión y los toast de Bootstrap con reconexión automática. La URL de destino quedó persistida en el modelo mediante una migración.
