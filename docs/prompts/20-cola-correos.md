# Cola de correos en segundo plano

## Prompt

Lee AGENTS.md. Rama feature/cola-correos. Implementa envío de correos en segundo plano con una cola de mensajes:

1. Configuración desde CloudAMQP:Url, Resend:ApiKey y Resend:CorreoDemo (nunca en el código). Si falta alguna, la app funciona igual: registra una advertencia y no envía correos.
2. Productor: Services/ColaCorreos con PublicarAsync(CorreoMensaje) que publique JSON (destinatario, asunto, plantilla, datos) en la cola durable "unilink.correos" de CloudAMQP usando RabbitMQ.Client (versión compatible con .NET 10). Mensajes persistentes.
3. Consumidor: Services/ConsumidorCorreos como BackgroundService que lee la cola y envía cada correo con la API de Resend (HttpClient a https://api.resend.com/emails, remitente "UniLink <onboarding@resend.dev>"). Confirma (ack) solo si se envió; si falla, reintenta hasta 3 veces y luego descarta registrando el error. Con prefetch de 1.
4. Modo demo: como Resend gratuito solo permite enviar al correo registrado, si existe Resend:CorreoDemo todos los correos se envían a ese correo, indicando en el asunto el destinatario real, por ejemplo "[para ana@usmp.pe] Tu solicitud fue aprobada".
5. Eventos que publican un correo (además de la notificación SignalR existente): nueva solicitud de alquiler recibida, solicitud aprobada o rechazada, reserva de clase confirmada (incluye el enlace de Jitsi) y recordatorio "Tu alquiler vence mañana", con un BackgroundService que revise una vez al día los alquileres Activos cuya FechaFin es mañana.
6. Plantillas HTML simples y responsivas con los colores de UniLink y un botón que lleve al detalle.
7. Amplía GET /health con "cola": "conectada" o "sin conexión".
8. Agrega GET /api/correos/prueba ([Authorize]) que publique un correo de prueba en la cola, para demostrarlo desde /scalar.
9. Ejecuta dotnet build. Crea docs/prompts/20-cola-correos.md con este prompt y un resumen. No ejecutes git ni muestres secretos.

## Resumen

- Se agregó RabbitMQ.Client 7.2.2, compatible con .NET 10, y el productor publica JSON persistente en la cola durable `unilink.correos`.
- Se implementó `ConsumidorCorreos` con prefetch de 1, ACK únicamente tras el envío de Resend y hasta tres reintentos antes de descartar y registrar el mensaje.
- Resend envía HTML responsivo desde `UniLink <onboarding@resend.dev>`. Con `Resend:CorreoDemo` configurado, envía al correo demo y antepone el correo destinatario real al asunto.
- Los eventos de solicitud/alquiler y confirmación de reserva de clase publican correos sin reemplazar las notificaciones SignalR. El correo de confirmación de clase incluye el enlace de reunión.
- Se agregó un servicio diario para publicar recordatorios de alquileres activos con vencimiento al día siguiente. Configura `App:BaseUrl` para que su botón de detalle use un enlace absoluto.
- `/health` informa el estado de la conexión de CloudAMQP y `GET /api/correos/prueba` permite encolar un correo de la cuenta autenticada desde Scalar.
- Si faltan `CloudAMQP:Url` o `Resend:ApiKey`, se registra una advertencia y la aplicación permanece operativa, sin enviar correos. `Resend:CorreoDemo` es opcional; si no está, se usa el destinatario real.
