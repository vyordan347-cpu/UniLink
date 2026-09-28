# 01 — Setup del proyecto

- **Rama:** feature/setup-proyecto
- **Agente:** GitHub Copilot (modo Agent, modelo Auto) en VS Code
- **Objetivo:** crear el proyecto ASP.NET Core 10 MVC y la landing de UniLink

## Prompt utilizado

Lee el archivo AGENTS.md para entender el proyecto. Estamos en la rama feature/setup-proyecto. Tu tarea es SOLO lo siguiente:

1. En la carpeta raíz actual (NO en una subcarpeta), crea un proyecto ASP.NET Core MVC con .NET 10 llamado UniLink, ejecutando: dotnet new mvc -n UniLink -o . --framework net10.0
2. NO modifiques ni borres AGENTS.md, README.md ni .gitignore.
3. En wwwroot/css/site.css define variables CSS con los colores de AGENTS.md (--ul-primario, --ul-accion, --ul-solidario, --ul-fondo) y úsalas en el diseño.
4. Personaliza Views/Shared/_Layout.cshtml: título "UniLink", barra de navegación con el nombre "UniLink" en color primario y enlaces Inicio, Explorar, Solidario, Iniciar sesión (por ahora apuntan a "#", excepto Inicio), y un pie de página "UniLink · Comunidad USMP · Conecta, comparte y aprende". Debe verse bien en móvil y escritorio (Bootstrap 5).
5. Reemplaza el contenido de Views/Home/Index.cshtml por una landing simple y moderna con: hero con título, eslogan y explicación del problema y la solución; botones "Iniciar sesión" y "Crear cuenta"; sección "¿Cómo funciona?" con 3 pasos; sección con los dos pilares (Materiales y Clases); franja "UniLink Solidario ❤️" en color coral.
6. Usa ViewData["Title"] = "Inicio" en esa vista.
7. Ejecuta dotnet build y confirma que compila sin errores. Si hay errores, corrígelos.
8. NO ejecutes ningún comando de git.

## Resultado

- Proyecto MVC .NET 10 creado en la raíz (UniLink.csproj, Program.cs, Controllers, Views, wwwroot).
- Layout, landing y estilos personalizados con la identidad visual de UniLink.
- `dotnet build` sin errores; landing probada en http://localhost:5055 (escritorio y móvil).

## Revisión humana

- Verifiqué en el navegador que la landing se ve correctamente y es responsiva.
- Los commits los realicé manualmente.