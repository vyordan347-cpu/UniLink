# UniLink

## Asistente UniLink

El asistente intenta Groq con `openai/gpt-oss-20b` y recurre a Gemini si Groq no está configurado o falla. Configura las claves fuera del repositorio:

- Desarrollo: `dotnet user-secrets set "Groq:ApiKey" "TU_CLAVE"` y/o `dotnet user-secrets set "Gemini:ApiKey" "TU_CLAVE"`.
- Render: define `Groq__ApiKey` y/o `Gemini__ApiKey`.

Si no hay ninguna clave, el chat informa amablemente que no está disponible. Los modelos se configuran mediante `Asistente:ModeloGroq`, `Asistente:Modelo` y `Asistente:ModeloAlternativo`.