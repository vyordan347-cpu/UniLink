# Funcionalidad de clases

- **Rama:** `feature/clases`
- **Agente:** Copilot SDK en VS Code
- **Skill usada:** `diseno-unilink`

## Prompt

Lee AGENTS.md y usa la skill diseno-unilink. Estamos en la rama feature/clases. Implementa la funcionalidad de CLASES con ClasesController y vistas en español:

1. Explorar clases (GET /Clases, público): tarjetas con curso, tutor (nombre y carrera), modalidad (Google Meet/Zoom), precio "S/ X por hora" o badge coral "GRATUITO". Filtros por curso (texto), carrera y "Solo gratuitas". En la vista Explorar de materiales agrega pestañas "Materiales | Clases" para cambiar entre ambas.
2. Detalle de clase (GET /Clases/Detalle/{id}, público): descripción, tutor, modalidad y precio. Botón "Reservar clase" (coral "Reservar clase solidaria" si es gratuita).
3. Ofrecer clase (GET/POST /Clases/Ofrecer, [Authorize]): curso, descripción, modalidad, enlace de la reunión (URL válida), precio por hora y checkbox "Ofrecer gratis como clase solidaria" que oculta el precio (se guarda en 0). Editar y eliminar solo para el tutor.
4. Reservar ([Authorize]): agrega a ReservaClase la propiedad DuracionHoras (int, 1 a 3) y CostoTotal (decimal 10,2) con migración. Formulario con fecha y hora (no pasadas), duración, campo opcional "¿Qué tema necesitas reforzar?", cálculo en vivo del costo (horas × PrecioHora), checkbox de condiciones (validación en servidor + JS simple; propiedades de navegación con [ValidateNever]). Reglas: no reservar tu propia clase; costo calculado en el servidor; Estado Pendiente. Luego pantalla "¡Reserva enviada!".
5. Mis clases (GET /Clases/Mias, [Authorize]) con pestañas "Como alumno" y "Como tutor": próximas e historial con badges de estado. El enlace de la reunión SOLO se muestra al alumno cuando la reserva está Aprobada.
6. Reservas recibidas (como tutor): Aprobar / Rechazar (al aprobar, rechaza automáticamente otras pendientes a la misma fecha y hora) y "Marcar como dictada" (Devuelto se muestra como "Dictada" en clases). Solo el tutor puede cambiar estados. Cada acción crea una Notificacion en la BD y usa TempData.
7. Página Solidario (GET /Solidario, pública): título "UniLink Solidario ❤️", texto "Comparte lo que tienes. Ayuda a otro estudiante.", y los materiales y clases gratuitos en dos secciones. Conecta el enlace "Solidario" del menú.
8. Panel: "Clases próximas" con datos reales. En "Mi actividad" agrega "Mis clases" y en el botón Publicar un menú con "Publicar material" y "Ofrecer clase".
9. Ajustes de diseño: el saludo del panel usa solo el primer nombre; los botones de acceso rápido del panel deben tener un estilo consistente (uno primario, el resto secundarios); la flecha del avatar debe ir al costado, no debajo.
10. Ejecuta dotnet build sin errores y aplica la migración.
11. Crea docs/prompts/09-clases.md con la rama, el agente, la skill usada, este prompt y un resumen.
12. NO ejecutes git ni muestres secretos.

## Resumen

Se implementó la exploración pública de clases, detalle, publicación y edición por el tutor, reservas con duración, costo calculado en servidor y validación de condiciones, pestañas de actividad para alumnos y tutores con gestión de solicitudes y notificaciones, y el catálogo UniLink Solidario. Se añadieron el endpoint JSON de clases consumido por los filtros, la navegación y los accesos del panel, junto con la migración de `ReservaClase`. Se ejecutaron la compilación y la aplicación de la migración.

## Correcciones

Usa la skill diseno-unilink. Correcciones en la funcionalidad de clases:

1. ESTADOS: en todas las vistas de clases muestra los estados con etiquetas propias: Aprobado → "Confirmada", Devuelto → "Dictada", Rechazado → "Rechazada", Pendiente → "Pendiente". Los botones del tutor deben ser "Confirmar", "Rechazar" y "Marcar como dictada". Crea un helper reutilizable para estas etiquetas y badges. No cambies los textos de alquileres.
2. CLASE GRATUITA: corrige que no se pueda publicar una clase solidaria. Si EsSolidaria es true, el precio no se valida y se guarda en 0 (validación en el servidor; quita cualquier Range/Required que lo impida). Revisa lo mismo en publicar material solidario.
3. ENLACE DE REUNIÓN:
   - Quita EnlaceReunion del formulario Ofrecer clase y de la entidad Clase. La modalidad pasa a ser: Videollamada UniLink (Jitsi), Google Meet o Zoom.
   - Agrega EnlaceReunion (string, opcional) a ReservaClase.
   - Al confirmar una reserva, genera automáticamente un enlace único de Jitsi: https://meet.jit.si/UniLink-{Curso sin espacios}-{Guid corto de 8 caracteres}.
   - En la pantalla del tutor, al confirmar, muestra un campo opcional "¿Prefieres tu propio enlace de Meet o Zoom?"; si lo llena con una URL válida, se usa esa en lugar de la de Jitsi.
   - El alumno ve el botón "Unirse a la clase" solo si la reserva está Confirmada; el tutor también ve el enlace.
   - Crea y aplica la migración correspondiente.
4. Ejecuta dotnet build sin errores.
5. Agrega al final de docs/prompts/09-clases.md una sección "Correcciones" con este prompt.
6. NO ejecutes git.

### Resumen de correcciones

Se añadieron etiquetas y badges reutilizables para los estados de reservas de clases, se corrigió la validación del precio de ofertas solidarias de clases y materiales, y se trasladó el enlace de reunión a cada reserva. Al confirmar, se crea un enlace Jitsi único o se usa el enlace opcional del tutor. Se agregó la modalidad Videollamada UniLink (Jitsi) y se creó y aplicó la migración de enlaces por reserva.
