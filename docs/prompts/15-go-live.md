# Preparación para lanzamiento

- **Rama:** `feature/go-live`
- **Agente:** Copilot SDK en VS Code
- **Skill usada:** `diseno-unilink`

## Prompt

Lee AGENTS.md y usa la skill diseno-unilink. Estamos en la rama feature/go-live. Prepara el lanzamiento:
1. Datos de demostración: un DataSeeder que, solo si la base está vacía de materiales, cree 3 usuarios de prueba (ana@usmp.pe, diego@usmp.pe, valeria@usmp.pe con contraseña Demo123!) de Odontología, Medicina e Ingeniería, 12 materiales realistas (con precio y solidarios), 6 clases, algunos alquileres y reseñas. Que sea idempotente.
2. Páginas de error amigables 404 y 500 con el diseño de UniLink (UseStatusCodePagesWithReExecute y UseExceptionHandler).
3. Página de Perfil público (/Perfil/{id}): nombre, carrera, reputación, insignia "Estudiante Solidario" si ofreció algo gratuito, materiales, clases y reseñas.
4. SEO y detalles: meta description, favicon con el ícono de UniLink, títulos por página y Open Graph básico.
5. Revisa todas las vistas en móvil (390px) y corrige lo que se vea mal.
6. Ejecuta dotnet build.
7. Crea docs/prompts/15-go-live.md con este prompt y un resumen.
8. NO ejecutes git.

## Resumen

Se agregó un seeder transaccional e idempotente que se detiene si ya hay materiales y usa ASP.NET Core Identity para guardar las contraseñas mediante hash. Se incorporaron páginas amigables para 404 y 500, un perfil público con reputación, ofertas y reseñas, metadatos de descripción/Open Graph y un favicon SVG. Se corrigió el ancho mínimo de un campo de reserva para evitar desbordes en tarjetas móviles. Las rutas públicas revisadas a 390 px no mostraron desbordamiento horizontal.

## Validación

- `dotnet build UniLink.csproj`: correcto, 0 advertencias y 0 errores.
- Navegador a 390 px: inicio, materiales, clases, Solidario, login, registro, privacidad, perfil público y páginas 404/500; las páginas observadas no presentaron desbordamiento horizontal.
