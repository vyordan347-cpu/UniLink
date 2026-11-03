# Funcionalidad de materiales

- **Rama:** `feature/materiales`
- **Agente:** Copilot SDK en VS Code

## Prompt

Lee AGENTS.md. Estamos en la rama feature/materiales. Implementa la funcionalidad de MATERIALES con MaterialesController y vistas Razor en español, Bootstrap 5 responsivo y la identidad visual de AGENTS.md:

1. Explorar (GET /Materiales, público, sin login): grilla de tarjetas (imagen, nombre, categoría, carrera, condición, ubicación, precio "S/ X por día" o badge coral "GRATUITO", badge "Disponible"/"Prestado", botón "Ver detalle"). Filtros por categoría (usar CategoriaService con caché Redis), carrera, precio máximo y checkbox "Solo gratuitos". Guarda el último filtro usado en una cookie "ul_filtro_materiales" (7 días) y aplícalo al volver. Estado vacío amigable si no hay resultados.
2. Detalle (GET /Materiales/Detalle/{id}, público): toda la información, badge "Esterilizado" si aplica, datos del propietario (nombre y carrera). Botón "Solicitar alquiler" deshabilitado por ahora con el texto "Disponible pronto" (se implementa en otra rama). Si el usuario es el propietario, muestra "Editar" y "Eliminar".
3. Publicar (GET/POST /Materiales/Publicar, [Authorize]): formulario con nombre, categoría, carrera, descripción, condición, checkbox "Está esterilizado", ubicación, URL de imagen (opcional; si está vacía usar una imagen por defecto en wwwroot/img), precio por día, y checkbox coral "Quiero ofrecerlo como préstamo solidario" que con JavaScript oculta el precio; si es solidario el precio se guarda en 0. Propietario = usuario logueado. Usa ViewModel con validaciones en español y antiforgery. Tras guardar, TempData "¡Material publicado!" y redirige al detalle.
4. Editar y Eliminar ([Authorize], solo el propietario; si no lo es, devolver Forbid). Eliminar con modal de confirmación y TempData.
5. Mis materiales (GET /Materiales/Mios, [Authorize]): lista de mis publicaciones con acciones.
6. Actualiza _Layout: "Explorar" apunta a /Materiales y, si hay sesión, agrega "Publicar" y "Mis materiales". En el Panel agrega accesos a Explorar y Publicar.
7. Usa async/await y consultas eficientes (Include del propietario y la categoría).
8. Ejecuta dotnet build sin errores.
9. Crea docs/prompts/06-materiales.md con la rama, el agente, este prompt completo y un resumen.
10. NO ejecutes git ni muestres secretos.

## Resumen

Se agregó el flujo MVC público y autenticado para explorar, filtrar, consultar, publicar, editar, eliminar y administrar materiales propios. La exploración conserva sus filtros durante siete días en una cookie; las categorías se obtienen mediante `CategoriaService`; los formularios incluyen validación, antiforgery y el comportamiento solidario; las vistas usan Bootstrap 5 y los colores de UniLink. La navegación del layout y el panel ahora enlazan a las secciones de materiales.
