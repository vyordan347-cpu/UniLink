# 04 — Autenticación con ASP.NET Core Identity

- **Rama:** feature/identity
- **Agente usado:** AI assistant using Copilot SDK in VS Code

## Prompt completo

Lee AGENTS.md. Estamos en la rama feature/identity. Implementa autenticación con ASP.NET Core Identity:

1. Instala Microsoft.AspNetCore.Identity.EntityFrameworkCore (compatible con .NET 10).
2. Haz que Usuario herede de IdentityUser<int>. Conserva Nombre, CodigoAlumno, Carrera, Ciclo y FechaRegistro; elimina Correo y PasswordHash porque Identity ya tiene Email y PasswordHash. Actualiza referencias e índices (CodigoAlumno sigue siendo único).
3. UniLinkDbContext debe heredar de IdentityDbContext<Usuario, IdentityRole<int>, int> y llamar a base.OnModelCreating.
4. Registra Identity en Program.cs con cookies de autenticación (LoginPath /Cuenta/Login), contraseña mínima de 6 caracteres, email único. Agrega también AddSession y UseSession.
5. Crea CuentaController con vistas propias en español y Bootstrap (no uses el scaffold de Identity UI):
   - GET/POST Registro: Nombre, Correo, CodigoAlumno, Carrera, Ciclo, Contraseña y Confirmar. Valida que el correo termine en @usmp.pe con el mensaje "Solo se permiten correos institucionales @usmp.pe".
   - GET/POST Login con checkbox "Recordarme" (cookie persistente).
   - POST Logout.
   - Tras el login, guarda el nombre del usuario en la Session ("NombreUsuario").
   - Usa TempData para mensajes: "¡Bienvenido a UniLink!", "Cuenta creada correctamente", "Sesión cerrada".
6. Crea PanelController con [Authorize] y la vista Index: "Hola, {nombre de la Session} 👋" y 3 tarjetas de estadísticas en 0 pasadas por ViewData (Alquileres activos, Clases próximas, Solicitudes pendientes).
7. En _Layout: si el usuario está autenticado, muestra "Mi panel" y "Cerrar sesión"; si no, "Iniciar sesión" y "Crear cuenta". Muestra los mensajes de TempData como alertas Bootstrap. Conecta los botones de la landing a Registro y Login.
8. Crea la migración "Identity" y aplícala con dotnet ef database update.
9. Ejecuta dotnet build sin errores.
10. Crea el archivo docs/prompts/04-identity.md con: rama, agente usado, este prompt completo y un resumen de lo que hiciste.
11. NO ejecutes comandos de git. NO muestres secretos.

## Resumen

- Se instaló Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.14 y se integró Identity con usuarios basados en `Usuario`.
- Se agregaron formularios y validaciones propias para registro e inicio de sesión, sesión con el nombre del usuario, cierre de sesión protegido y mensajes mediante TempData.
- Se creó el panel protegido con sus tres estadísticas inicializadas en cero y se conectaron sus rutas desde la navegación y la landing.
- Se generó la migración `Identity`, que conserva el correo y las contraseñas hash existentes al adaptar la tabla de usuarios.
