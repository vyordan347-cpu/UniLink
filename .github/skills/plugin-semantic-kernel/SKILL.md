---
name: plugin-semantic-kernel
description: Guía para agregar y probar plugins seguros de solo lectura al Asistente UniLink con Semantic Kernel.
---

# Agregar plugins al Asistente UniLink

## Alcance

Usa esta guía cuando agregues una herramienta nueva al Asistente UniLink con Semantic Kernel. Los plugins consultan la información de UniLink y son estrictamente de solo lectura.

## Convenciones

- Crea las clases dentro de `Agente/Plugins` y usa nombres de clases y funciones en español.
- Marca cada función invocable con `[KernelFunction("NombreEnEspañol")]` y añade `[Description("...")]` en español a la función y a cada parámetro visible para el modelo.
- Inyecta `UniLinkDbContext` y consulta con `AsNoTracking()` y métodos asíncronos de Entity Framework Core. Para datos de usuario, recibe el identificador autenticado al construir el plugin; nunca confíes en un identificador elegido por el modelo.
- No crees, actualices ni elimines entidades. El agente no puede publicar, solicitar, aprobar, rechazar ni borrar datos.
- Devuelve texto breve y verificable. Las búsquedas deben limitarse a un máximo de cinco resultados y cada resultado navegable debe incluir un enlace relativo a su detalle.
- No inventes registros, precios, enlaces ni estados. Informa claramente cuando no haya resultados.
- Reutiliza servicios de dominio existentes cuando corresponda, como `RecomendacionService`, en lugar de duplicar sus reglas.

## Registro

Registra cada instancia en `Services/AsistenteService.cs` dentro del constructor del Kernel, mediante `kernel.Plugins.AddFromObject(instancia, "NombrePlugin")`. Mantén las consultas y el alcance del plugin visibles y fáciles de revisar.

## Verificación

1. Añade o actualiza pruebas para resultados vacíos, filtros, límite de cinco elementos, enlaces relativos y aislamiento por usuario.
2. Prueba que las llamadas del modelo seleccionen la función correcta usando la API del Asistente y una clave de Gemini configurada en user-secrets; no agregues claves al repositorio.
3. Prueba el modo sin clave y confirma que responde con el mensaje de indisponibilidad sin fallar el inicio de la aplicación.
4. Ejecuta `dotnet build` y las pruebas específicas del plugin.
