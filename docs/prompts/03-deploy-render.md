# 03 — Preparación para despliegue en Render

- **Rama:** feature/deploy-render
- **Agente:** GitHub Copilot (modo Agent, modelo Auto) en VS Code
- **Objetivo:** preparar la app para desplegarse en Render con Docker

## Prompt 1 utilizado

Lee AGENTS.md. Estamos en la rama feature/deploy-render. Vamos a preparar el proyecto para desplegarlo en Render usando Docker. Tu tarea es SOLO lo siguiente:

1. Crea un Dockerfile multi-etapa (sdk:10.0 para compilar, aspnet:10.0 para ejecutar) con comentarios en español.
2. Crea un .dockerignore que excluya bin/, obj/, .git/, .vs/, .vscode/, *.user, docs/ y archivos .env.
3. En Program.cs:
   a) Puerto dinámico leyendo la variable de entorno PORT en tiempo de ejecución (http://0.0.0.0:{PORT}).
   b) ForwardedHeaders (X-Forwarded-For y X-Forwarded-Proto) para funcionar detrás del proxy HTTPS de Render.
   c) Aplicar migraciones pendientes al iniciar con Database.Migrate() y try/catch con log.
   d) Endpoint GET /health que indique si hay conexión a la base de datos (CanConnectAsync).
4. La cadena de conexión llega por la variable de entorno ConnectionStrings__DefaultConnection. NO escribir secretos en el código.
5. Ejecutar dotnet build.
6. NO ejecutar comandos de git.

## Prompt 2 utilizado (corrección)

Corrige la advertencia de compilación: en .NET 10 la propiedad KnownNetworks de ForwardedHeadersOptions está obsoleta. Reemplázala por KnownIPNetworks manteniendo el mismo comportamiento. No cambies nada más.

## Resultado
- Dockerfile multi-etapa y .dockerignore creados.
- Program.cs con puerto dinámico, forwarded headers, migraciones automáticas y endpoint /health.
- Advertencia de KnownNetworks corregida.
- `dotnet build` sin errores ni advertencias.

## Revisión humana
- Probé localmente la landing y `/health` → {"estado":"ok","baseDeDatos":"conectada"}.
- Los commits los realicé manualmente.