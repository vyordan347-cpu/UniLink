# 05 — Redis para sesiones y caché

- **Rama:** feature/redis-session
- **Agente usado:** AI assistant using Copilot SDK in VS Code

## Prompt completo

Lee AGENTS.md. Estamos en la rama feature/redis-session. Integra Redis:

1. Instala Microsoft.Extensions.Caching.StackExchangeRedis y Microsoft.AspNetCore.DataProtection.StackExchangeRedis (compatibles con .NET 10).
2. En Program.cs lee GetConnectionString("Redis"):
   - Si existe: usa AddStackExchangeRedisCache (InstanceName "UniLink:") para que las sesiones se guarden en Redis, y persiste las claves de Data Protection en Redis (PersistKeysToStackExchangeRedis) para que las cookies de login sigan válidas tras reinicios en Render.
   - Si no existe: usa AddDistributedMemoryCache y registra una advertencia en el log.
3. Crea Services/CategoriaService.cs que obtenga las categorías usando IDistributedCache: primero busca en caché (clave "categorias", 10 minutos); si no están, las lee de la base de datos y las guarda. Registra en el log si vinieron de "caché" o de "base de datos".
4. En el Panel muestra las categorías obtenidas con ese servicio, como chips o badges.
5. Amplía GET /health para incluir redis: "conectado" o "sin conexión".
6. NO escribas secretos en appsettings.json ni en el código. NO muestres secretos.
7. Ejecuta dotnet build sin errores.
8. Crea docs/prompts/05-redis-session.md con la rama, el agente, este prompt completo y un resumen.
9. NO ejecutes comandos de git.

## Resumen

- Se agregaron Microsoft.Extensions.Caching.StackExchangeRedis y Microsoft.AspNetCore.DataProtection.StackExchangeRedis en la versión 10.0.14.
- Las sesiones y la caché distribuida usan Redis si está configurado; si falta la conexión se usa memoria y se registra una advertencia.
- Las claves de Data Protection se persisten en Redis para mantener válidas las cookies de autenticación después de reinicios.
- Se creó un servicio que obtiene las categorías de la caché o de PostgreSQL, las almacena durante 10 minutos y registra su origen.
- El Panel presenta las categorías como badges y `/health` informa el estado de Redis.

## Corrección /health

Lee AGENTS.md. Estamos en la rama fix/health-redis. El endpoint GET /health solo devuelve estado y baseDeDatos. Amplíalo para que también incluya "redis": "conectado" o "sin conexión", verificando la conexión real a Redis (por ejemplo, escribiendo y leyendo una clave de prueba con IDistributedCache dentro de un try/catch con timeout corto). Si no hay cadena de Redis configurada, devuelve "redis": "no configurado". El endpoint no debe lanzar excepciones aunque Redis falle. Ejecuta dotnet build. Agrega al final de docs/prompts/05-redis-session.md una sección "Corrección /health" con este prompt. No ejecutes git ni muestres secretos.
