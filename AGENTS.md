# AGENTS.md — Contexto del proyecto UniLink

## Qué es UniLink
Plataforma web responsiva para estudiantes de la USMP (Universidad de San Martín de Porres, Perú).
Eslogan: "Conecta, comparte y aprende."

Dos pilares:
1. **Materiales:** los estudiantes se alquilan entre ellos materiales académicos caros (instrumental de Odontología, equipos de Medicina, libros, calculadoras). Cada dueño fija su precio por día o lo ofrece gratis.
2. **Clases:** los estudiantes ofrecen clases de un curso, pagadas por hora o gratuitas, dictadas por Google Meet o Zoom. La app solo gestiona la oferta, la reserva y el enlace; NO hace video.

**UniLink Solidario:** materiales y clases gratuitas (precio = 0) para estudiantes con dificultades económicas.

## Reglas de negocio
- Solo se registran estudiantes con correo que termine en `@usmp.pe`.
- Los pagos son en persona (efectivo, Yape o Plin). La app NO procesa pagos: solo calcula y muestra costos.
- PROHIBIDO: carrito de compras, checkout o pasarela de pago.
- Un usuario no puede solicitar su propio material ni reservar su propia clase.
- Costo de alquiler = días (contando inicio y fin) × PrecioDia. Ej.: del 01 al 03 = 3 días.
- Solo el dueño de un material o clase puede aprobar o rechazar solicitudes, editar o eliminar.
- Estados de Alquiler y ReservaClase: Pendiente, Aprobado, Activo, Devuelto, Rechazado.
- Las contraseñas se guardan con hash (nunca en texto plano).

## Stack técnico
- .NET 10, ASP.NET Core MVC con vistas Razor, C#.
- Entity Framework Core con PostgreSQL (proveedor Npgsql) y migraciones.
- Redis (StackExchange.Redis) para sesiones distribuidas y caché.
- SignalR para notificaciones en tiempo real.
- Bootstrap 5 para un diseño responsivo (móvil y escritorio).
- Despliegue en Render con Dockerfile.

## Entidades (nombres en español)
- **Usuario:** Id, Nombre, Correo, CodigoAlumno, Carrera, Ciclo, PasswordHash, FechaRegistro
- **Categoria:** Id, Nombre
- **Material:** Id, Nombre, Descripcion, Carrera, CategoriaId, Condicion, Esterilizado, Ubicacion, PrecioDia, EsSolidario, ImagenUrl, Disponible, PropietarioId, FechaPublicacion
- **Alquiler:** Id, MaterialId, SolicitanteId, FechaInicio, FechaFin, CostoTotal, Estado, FechaSolicitud
- **Clase:** Id, Curso, Descripcion, Modalidad, EnlaceReunion, PrecioHora, EsSolidaria, TutorId, FechaPublicacion
- **ReservaClase:** Id, ClaseId, AlumnoId, FechaHora, Estado, TemaAReforzar
- **Notificacion:** Id, UsuarioId, Mensaje, Leida, Fecha

## Tópicos técnicos que el código DEBE demostrar
- Base de datos con EF Core + PostgreSQL y migraciones.
- Métodos HTTP GET, POST, PUT y DELETE.
- Sesiones (usuario logueado), TempData (mensajes tras redirigir), ViewData (títulos y estadísticas), Cookies ("Recordarme" y último filtro usado).
- Redis (sesiones y caché de categorías).
- API REST en `/api/materiales` y `/api/clases`, consumida con `fetch` desde la vista Explorar.
- WebSocket con SignalR (notificaciones al recibir o aprobar solicitudes).

## Convenciones de código
- Nombres de clases, propiedades, controladores y vistas en español (ej. `MaterialesController`, `Alquiler`).
- Código simple, legible y con comentarios breves en español: el desarrollador es principiante.
- Usar async/await en el acceso a datos.
- Validaciones con Data Annotations y mensajes de error en español.
- Diseño visual: primario índigo #3B4CCA, acción turquesa #14B8A6, solidario coral #F97362 (solo para lo gratuito), fondo #F7F8FC, tarjetas blancas con bordes redondeados.

## Seguridad y configuración (OBLIGATORIO)
- NUNCA escribir credenciales (cadenas de conexión de PostgreSQL o Redis, API keys) en `appsettings.json` ni en el código.
- En desarrollo se usan `dotnet user-secrets`; en Render, variables de entorno.
- El puerto en producción se lee en tiempo de ejecución desde la variable de entorno `PORT`.

## Reglas para el agente
- NO ejecutar comandos de Git (commit, push, merge, checkout). El desarrollador los hace manualmente.
- Hacer SOLO lo que pide el prompt actual; no adelantar funcionalidades de otras ramas.
- Al terminar, resumir qué archivos se crearon o modificaron y cómo probar el resultado.
- Verificar que el proyecto compile con `dotnet build` antes de terminar.

## Fases futuras (NO implementar todavía)
- Chatbot con IA para ayudar a los estudiantes a encontrar materiales y clases.