# Funcionalidad de alquileres

- **Rama:** `feature/alquileres`
- **Agente:** Copilot SDK en VS Code

## Prompt

Lee AGENTS.md. Estamos en la rama feature/alquileres. Implementa la funcionalidad de ALQUILERES con AlquileresController, vistas Razor en español y Bootstrap responsivo:

1. Solicitar alquiler ([Authorize]): en el detalle del material, reemplaza el botón deshabilitado por "Solicitar alquiler" (o "Solicitar préstamo solidario" en coral si EsSolidario). Lleva a GET /Alquileres/Solicitar/{materialId} con: resumen del material, fecha de inicio y fecha de devolución, cálculo en vivo con JavaScript de días (inclusivos) y costo total (días × PrecioDia), lugar de entrega, nota "El pago se realiza en persona al recoger (efectivo, Yape o Plin)", y checkbox obligatorio "He leído y acepto las condiciones del préstamo". Botones "Confirmar solicitud" y "Cancelar".
2. POST Solicitar con validaciones en el servidor: no solicitar tu propio material; la fecha de inicio no puede ser pasada; la de devolución debe ser >= la de inicio; el material debe estar Disponible; no puede haber otro alquiler Aprobado o Activo que se cruce en esas fechas. Calcula CostoTotal en el servidor (nunca confiar en el valor del cliente). Estado = Pendiente.
3. Confirmación (GET /Alquileres/Confirmacion/{id}): "¡Solicitud enviada!", "Tu solicitud está pendiente de aprobación por el dueño." y el resumen con el badge de estado.
4. Mis alquileres (GET /Alquileres, [Authorize]): pestañas Pendientes, Activos (Aprobado y Activo) e Historial (Devuelto y Rechazado), con tarjetas que muestren material, fechas, costo y badge de estado con colores (Pendiente ámbar, Aprobado azul, Activo turquesa, Devuelto gris, Rechazado rojo). En los activos muestra "Faltan X días para devolverlo".
5. Solicitudes recibidas (GET /Alquileres/Recibidas, [Authorize], solo materiales propios): lista con estudiante (nombre y carrera), material, fechas, días, costo y botones:
   - Pendiente: "Aprobar" y "Rechazar". Al aprobar, rechaza automáticamente las otras solicitudes Pendientes del mismo material que se crucen en fechas.
   - Aprobado: "Marcar como entregado" (pasa a Activo y el material a Disponible = false).
   - Activo: "Marcar como devuelto" (pasa a Devuelto y el material a Disponible = true).
   Solo el propietario puede cambiar estados (si no, Forbid). Usa antiforgery y TempData en cada acción.
6. Cada cambio de estado y cada nueva solicitud crea un registro en la tabla Notificacion para el usuario afectado (ej. "Recibiste una solicitud de Ana para Kit de operatoria", "Tu solicitud de Kit de operatoria fue aprobada"). Solo guardarlas en la base de datos; el tiempo real se hará en otra rama.
7. Panel: las estadísticas ViewData muestran datos reales (alquileres activos, solicitudes pendientes recibidas). En _Layout agrega "Mis alquileres" y "Solicitudes" si hay sesión.
8. Ejecuta dotnet build sin errores. Si necesitas una migración, créala y aplícala.
9. Crea docs/prompts/07-alquileres.md con la rama, el agente, este prompt completo y un resumen.
10. NO ejecutes git ni muestres secretos.

## Resumen

Se implementó el flujo de solicitud de alquiler con cálculo inclusivo y validación de fechas, disponibilidad, propiedad y cruces confirmados. Se agregaron confirmación, las pestañas de alquileres propios y la bandeja de solicitudes recibidas con aprobación, rechazo, entrega y devolución protegidas por antiforgery y verificación de propiedad. Las solicitudes y cambios de estado generan notificaciones persistidas, se actualizaron navegación y estadísticas del panel y no se requirió migración porque las tablas ya existían.
