# Agente UniLink con Semantic Kernel

- **Rama solicitada:** `feature/agente-sk`
- **Agente:** AI assistant using Copilot SDK in VS Code

## Prompt

Lee AGENTS.md y usa la skill diseno-unilink. Estamos en la rama feature/agente-sk. Implementa un agente de IA con Semantic Kernel:

1. Instala Microsoft.SemanticKernel y el conector de Google Gemini (Microsoft.SemanticKernel.Connectors.Google, versión prerelease compatible). Modelo configurable en appsettings "Asistente:Modelo" (por defecto un modelo Gemini Flash vigente). La API key se lee de configuración "Gemini:ApiKey" (user-secrets en local, variable de entorno Gemini__ApiKey en Render). NUNCA escribas la key en el código ni en appsettings. Si no hay key, el asistente responde amablemente que no está disponible, sin romper la app.
2. Plugins en la carpeta Agente/Plugins, con [KernelFunction] y [Description] en español en cada función y parámetro:
   - MaterialesPlugin: BuscarMateriales(texto, carrera, precioMaximo, soloGratuitos) y ObtenerDetalleMaterial(id).
   - ClasesPlugin: BuscarClases(curso, soloGratuitas).
   - MiActividadPlugin (usuario actual): MisAlquileresActivos(), MisProximasClases(), MisSolicitudesPendientes().
   - CostosPlugin: CalcularCostoAlquiler(materialId, fechaInicio, fechaFin), reutilizando la regla de días inclusivos.
   - RecomendacionesPlugin: RecomendarParaMi(), usando RecomendacionService.
   Todos los plugins son de SOLO LECTURA: el agente nunca crea, modifica ni elimina datos. Devuelven resultados resumidos (máximo 5) con enlaces relativos a cada detalle.
3. Agente/AsistenteService: construye el Kernel por petición con los plugins y FunctionChoiceBehavior.Auto(). Prompt de sistema: "Eres el Asistente UniLink, ayudas a estudiantes de la USMP a encontrar materiales y clases. Responde en español, breve y amable. Usa SIEMPRE las herramientas para datos reales; nunca inventes materiales, precios ni clases. Recuerda que los pagos son en persona y no hay carrito. Si te piden algo fuera de UniLink, redirige amablemente." Guarda el historial del chat por usuario en Redis (máximo 20 mensajes, expira en 2 horas).
4. Endpoint POST /api/asistente/chat ([Authorize]) que reciba { mensaje } y devuelva { respuesta, herramientasUsadas }. Limita a 20 mensajes por usuario cada 10 minutos (rate limiting de ASP.NET Core) y responde 429 con un mensaje amable si se excede. Agrega POST /api/asistente/reiniciar para borrar el historial.
5. UI: botón flotante (abajo a la derecha) "Asistente UniLink ✨" visible con sesión, que abre un panel de chat: burbujas de usuario y asistente, indicador "escribiendo...", 3 sugerencias rápidas ("¿Qué materiales de Odontología hay?", "Clases gratis de Anatomía", "¿Qué me recomiendas?"), soporte de enlaces en las respuestas, botón para reiniciar la conversación y diseño responsivo según la skill. En las respuestas muestra discretamente qué herramientas usó (ej. "🔧 BuscarMateriales").
6. Crea una nueva skill en .github/skills/plugin-semantic-kernel/SKILL.md que explique, para futuros agentes, cómo agregar un nuevo plugin al Asistente UniLink (convenciones, descripciones en español, solo lectura, máximo 5 resultados, registrarlo en AsistenteService, probarlo).
7. Ejecuta dotnet build.
8. Crea docs/prompts/14-agente-sk.md con la rama, el agente, este prompt y un resumen con ejemplos de conversación.
9. NO ejecutes git ni muestres secretos.

## Multi-proveedor

### Prompt

Cambia el proveedor principal del Asistente UniLink a Groq, manteniendo Gemini como respaldo:

1. Usa el conector OpenAI de Semantic Kernel (Microsoft.SemanticKernel.Connectors.OpenAI) con el endpoint compatible https://api.groq.com/openai/v1. API key desde configuración "Groq:ApiKey" (nunca en el código).
2. Configuración en appsettings: "Asistente:Proveedor" = "Groq", "Asistente:ModeloGroq" = un modelo Llama de Groq que soporte function calling (por ejemplo, la versión 70B "versatile" vigente; verifica en la documentación de Groq), y conserva la configuración de Gemini.
3. Orden de intento: Groq → (si falla con 429/500/503 tras 1 reintento) Gemini → mensaje amable. Si una key no está configurada, salta ese proveedor. Registra en el log qué proveedor y modelo respondió.
4. Verifica que FunctionChoiceBehavior.Auto() funcione con Groq y que los plugins se invoquen (prueba "¿Qué materiales de Odontología hay?").
5. En el widget, muestra discretamente el proveedor usado (ej. "⚡ Groq · Llama").
6. Actualiza docs/prompts/14-agente-sk.md con una sección "Multi-proveedor" que incluya este prompt.
7. Ejecuta dotnet build. No ejecutes git ni muestres secretos.

### Resultado

- Se agregó el conector OpenAI de Semantic Kernel y se configura el endpoint compatible de Groq sin guardar la clave en el repositorio.
- El proveedor preferido es Groq y Gemini queda como respaldo; se omite automáticamente cualquier proveedor sin clave.
- Semantic Kernel usa `FunctionChoiceBehavior.Auto()` con los mismos plugins de solo lectura.
- El endpoint devuelve el proveedor y modelo usados para mostrarlo en el widget.
- `llama-3.3-70b-versatile` ya no es apropiado como valor predeterminado: Groq lo marca retirado desde el 16 de agosto de 2026. Se configuró `openai/gpt-oss-20b`, que la documentación de Groq mantiene con soporte para tool calling.

### Ejemplo de conversación

**Estudiante:** ¿Qué materiales de Odontología hay?

**Asistente:** El kernel de Groq procesa la consulta con `FunctionChoiceBehavior.Auto()`, invoca `BuscarMateriales` con el filtro de carrera y presenta los resultados reales con sus enlaces. El widget indica el proveedor y el modelo usados.

## Resumen de implementación

- Se añadieron cinco plugins de solo lectura para materiales, clases, actividad del usuario, costos y recomendaciones.
- `AsistenteService` construye un Kernel por petición, intenta Groq y utiliza Gemini como respaldo; conserva hasta 20 mensajes por usuario durante dos horas en la caché distribuida.
- Se añadieron el chat autenticado con límite de solicitudes y el endpoint para reiniciar la conversación.
- El panel flotante muestra respuestas, enlaces y las herramientas invocadas.
- Se documentó la convención para ampliar plugins en `.github/skills/plugin-semantic-kernel/SKILL.md`.

## Ejemplos de conversación

**Estudiante:** ¿Qué materiales de Odontología hay?

**Asistente:** Consulta `BuscarMateriales` y resume únicamente los materiales disponibles devueltos por la base de datos, con sus precios y enlaces relativos. Si no hay coincidencias, lo indica sin inventar resultados.

**Estudiante:** Clases gratis de Anatomía.

**Asistente:** Consulta `BuscarClases` con el filtro gratuito y el curso indicado, y enlaza las clases devueltas.

**Estudiante:** ¿Qué me recomiendas?

**Asistente:** Consulta `RecomendarParaMi` para obtener recomendaciones personalizadas y las presenta brevemente.

**Estudiante:** ¿Cuánto cuesta alquilar este material del 1 al 3 de diciembre?

**Asistente:** Si no conoce el identificador, primero busca el material con `BuscarMateriales`; después consulta `CalcularCostoAlquiler` y muestra el cálculo inclusivo (tres días), recordando que el pago se coordina en persona.
