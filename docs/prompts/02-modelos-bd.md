# 02 — Modelos y base de datos

- **Rama:** feature/modelos-bd
- **Agente:** GitHub Copilot (modo Agent, modelo Auto) en VS Code
- **Objetivo:** crear las entidades, el DbContext y la base de datos PostgreSQL con migraciones

## Preparación manual (sin agente)
- Base de datos PostgreSQL creada en Neon (plan gratuito).
- Cadena de conexión guardada con `dotnet user-secrets` como `ConnectionStrings:DefaultConnection` (nunca en el repositorio).

## Prompt utilizado

Lee AGENTS.md. Estamos en la rama feature/modelos-bd. Tu tarea es SOLO lo siguiente:

1. Instala los paquetes NuGet compatibles con .NET 10: Npgsql.EntityFrameworkCore.PostgreSQL y Microsoft.EntityFrameworkCore.Design. Instala o actualiza la herramienta global dotnet-ef.
2. En la carpeta Models crea las entidades definidas en AGENTS.md (Usuario, Categoria, Material, Alquiler, Clase, ReservaClase, Notificacion) con navegación, claves foráneas, enums (EstadoSolicitud, CondicionMaterial, ModalidadClase), fechas DateOnly/DateTime UTC, precios decimal(10,2) y Data Annotations en español.
3. Crea Data/UniLinkDbContext.cs con índices únicos (Correo, CodigoAlumno), relaciones con DeleteBehavior.Restrict y datos iniciales de Categoria.
4. En Program.cs registra el DbContext con UseNpgsql leyendo GetConnectionString("DefaultConnection").
5. SEGURIDAD: NO escribas cadenas de conexión en appsettings.json ni en el código. NO muestres el valor de la cadena.
6. Crea y aplica la migración: dotnet ef migrations add Inicial / dotnet ef database update
7. Ejecuta dotnet build.
8. NO ejecutes ningún comando de git.

## Resultado
- Paquetes: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0 y Microsoft.EntityFrameworkCore.Design 10.0.12.
- 7 entidades y 3 enums en Models/, UniLinkDbContext en Data/, migración Inicial en Migrations/.
- Migración aplicada en Neon: 7 tablas + __EFMigrationsHistory, índices únicos y 7 categorías iniciales.
- `dotnet build` sin errores.

## Revisión humana
- Verifiqué las tablas en la consola de Neon.
- Verifiqué con `git grep "Password="` que no hay credenciales en el repositorio.
- Los commits los realicé manualmente.