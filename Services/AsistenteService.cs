using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using UniLink.Agente.Plugins;
using UniLink.Data;

namespace UniLink.Services;

public sealed class AsistenteService(
    IConfiguration configuracion,
    IDistributedCache cache,
    UniLinkDbContext db,
    RecomendacionService recomendacionService,
    ILogger<AsistenteService> logger)
{
    private const string InstruccionesSistema =
        "Eres el Asistente UniLink, ayudas a estudiantes de la USMP a encontrar materiales y clases. " +
        "Responde en español, breve y amable. Usa SIEMPRE las herramientas para datos reales; nunca inventes materiales, precios ni clases. " +
        "Recuerda que los pagos son en persona y no hay carrito. Si te piden algo fuera de UniLink, redirige amablemente.";
    private static readonly TimeSpan DuracionHistorial = TimeSpan.FromHours(2);
    private static readonly TimeSpan EsperaReintento = TimeSpan.FromSeconds(1);
    private const int MaximoMensajes = 20;
    private static readonly Uri EndpointGroq = new("https://api.groq.com/openai/v1");

    public async Task<RespuestaAsistente> ResponderAsync(
        int usuarioId,
        string mensaje,
        CancellationToken cancellationToken)
    {
        var mensajes = await ObtenerHistorialAsync(usuarioId, cancellationToken);
        var proveedorPreferido = configuracion["Asistente:Proveedor"] ?? "Groq";
        var ordenProveedores = proveedorPreferido.Equals("Gemini", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Gemini", "Groq" }
            : new[] { "Groq", "Gemini" };

        foreach (var proveedor in ordenProveedores)
        {
            var apiKey = proveedor == "Groq"
                ? configuracion["Groq:ApiKey"]
                : configuracion["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogInformation("Se omite el proveedor {Proveedor}: no hay una API key configurada.", proveedor);
                continue;
            }

            var modelo = proveedor == "Groq"
                ? configuracion["Asistente:ModeloGroq"] ?? "openai/gpt-oss-20b"
                : configuracion["Asistente:Modelo"] ?? "gemini-3.8-flash";
            var respuesta = await IntentarProveedorAsync(
                proveedor,
                modelo,
                apiKey,
                usuarioId,
                mensajes,
                mensaje,
                cancellationToken);

            if (respuesta is null && proveedor == "Gemini")
            {
                var modeloAlternativo = configuracion["Asistente:ModeloAlternativo"] ?? "gemini-3.5-flash-lite";
                if (!modeloAlternativo.Equals(modelo, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Gemini no respondió con el modelo {Modelo}; se probará el modelo alternativo {ModeloAlternativo}.",
                        modelo,
                        modeloAlternativo);
                    respuesta = await IntentarProveedorAsync(
                        proveedor,
                        modeloAlternativo,
                        apiKey,
                        usuarioId,
                        mensajes,
                        mensaje,
                        cancellationToken);
                    if (respuesta is not null)
                    {
                        modelo = modeloAlternativo;
                    }
                }
            }

            if (respuesta is null)
            {
                continue;
            }

            logger.LogInformation(
                "El Asistente UniLink respondió usando {Proveedor} con el modelo {Modelo}.",
                proveedor,
                modelo);

            mensajes.Add(new MensajeChat("user", mensaje));
            mensajes.Add(new MensajeChat("assistant", respuesta.Contenido));
            await cache.SetStringAsync(
                CrearClaveHistorial(usuarioId),
                JsonSerializer.Serialize(mensajes.TakeLast(MaximoMensajes)),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DuracionHistorial
                },
                cancellationToken);

            return new RespuestaAsistente(
                respuesta.Contenido,
                respuesta.HerramientasUsadas,
                proveedor,
                modelo);
        }

        throw new AsistenteNoDisponibleException(
            new InvalidOperationException("No respondió ningún proveedor configurado."));
    }

    public Task ReiniciarAsync(int usuarioId, CancellationToken cancellationToken) =>
        cache.RemoveAsync(CrearClaveHistorial(usuarioId), cancellationToken);

    private async Task<RespuestaProveedor?> IntentarProveedorAsync(
        string proveedor,
        string modelo,
        string apiKey,
        int usuarioId,
        IReadOnlyList<MensajeChat> mensajes,
        string mensaje,
        CancellationToken cancellationToken)
    {
        for (var intento = 0; intento < 2; intento++)
        {
            try
            {
                var respuesta = await ObtenerRespuestaModeloAsync(
                    proveedor,
                    modelo,
                    apiKey,
                    usuarioId,
                    mensajes,
                    mensaje,
                    cancellationToken);
                return respuesta;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpOperationException exception) when (EsErrorTransitorio(exception))
            {
                if (intento == 1)
                {
                    logger.LogWarning(
                        exception,
                        "{Proveedor} agotó los dos intentos con el modelo {Modelo} (HTTP {Codigo}).",
                        proveedor,
                        modelo,
                        exception.StatusCode);
                    return null;
                }

                logger.LogWarning(
                    exception,
                    "{Proveedor} devolvió HTTP {Codigo} con el modelo {Modelo}; se reintentará una vez en {EsperaSegundos} segundo.",
                    proveedor,
                    exception.StatusCode,
                    modelo,
                    EsperaReintento.TotalSeconds);
                await Task.Delay(EsperaReintento, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Falló el proveedor {Proveedor} con el modelo {Modelo}; se probará el siguiente proveedor.",
                    proveedor,
                    modelo);
                return null;
            }
        }

        return null;
    }

    private async Task<RespuestaProveedor> ObtenerRespuestaModeloAsync(
        string proveedor,
        string modelo,
        string apiKey,
        int usuarioId,
        IReadOnlyList<MensajeChat> mensajes,
        string mensaje,
        CancellationToken cancellationToken)
    {
        var constructor = Kernel.CreateBuilder();
        if (proveedor == "Groq")
        {
            constructor.AddOpenAIChatCompletion(
                modelId: modelo,
                endpoint: EndpointGroq,
                apiKey: apiKey);
        }
        else
        {
            constructor.AddGoogleAIGeminiChatCompletion(modelId: modelo, apiKey: apiKey);
        }

        var herramientasUsadas = new HashSet<string>(StringComparer.Ordinal);
        var kernel = constructor.Build();
        kernel.FunctionInvocationFilters.Add(new FiltroHerramientas(herramientasUsadas));
        kernel.Plugins.AddFromObject(new MaterialesPlugin(db, usuarioId), "MaterialesPlugin");
        kernel.Plugins.AddFromObject(new ClasesPlugin(db, usuarioId), "ClasesPlugin");
        kernel.Plugins.AddFromObject(new MiActividadPlugin(db, usuarioId), "MiActividadPlugin");
        kernel.Plugins.AddFromObject(new CostosPlugin(db), "CostosPlugin");
        kernel.Plugins.AddFromObject(
            new RecomendacionesPlugin(recomendacionService, usuarioId),
            "RecomendacionesPlugin");

        var historial = new ChatHistory(InstruccionesSistema);
        foreach (var mensajeAnterior in mensajes)
        {
            if (mensajeAnterior.Rol == "user")
            {
                historial.AddUserMessage(mensajeAnterior.Contenido);
            }
            else
            {
                historial.AddAssistantMessage(mensajeAnterior.Contenido);
            }
        }

        historial.AddUserMessage(mensaje);
        var ajustes = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        var respuesta = await kernel.GetRequiredService<IChatCompletionService>()
            .GetChatMessageContentAsync(historial, ajustes, kernel, cancellationToken);
        if (string.IsNullOrWhiteSpace(respuesta.Content))
        {
            throw new InvalidOperationException("El proveedor del asistente devolvió una respuesta vacía.");
        }

        return new RespuestaProveedor(respuesta.Content, herramientasUsadas.ToArray());
    }

    private static bool EsErrorTransitorio(HttpOperationException exception) =>
        exception.StatusCode is HttpStatusCode.InternalServerError
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable;

    private async Task<List<MensajeChat>> ObtenerHistorialAsync(
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var contenido = await cache.GetStringAsync(CrearClaveHistorial(usuarioId), cancellationToken);
        return contenido is null
            ? []
            : JsonSerializer.Deserialize<List<MensajeChat>>(contenido)
                ?? throw new InvalidDataException("El historial del asistente contiene datos no válidos.");
    }

    private static string CrearClaveHistorial(int usuarioId) => $"asistente:historial:{usuarioId}";

    private sealed class FiltroHerramientas(HashSet<string> herramientas) : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(
            FunctionInvocationContext context,
            Func<FunctionInvocationContext, Task> next)
        {
            herramientas.Add(context.Function.Name);
            await next(context);
        }
    }

    private sealed record MensajeChat(string Rol, string Contenido);
    private sealed record RespuestaProveedor(string Contenido, IReadOnlyList<string> HerramientasUsadas);
}

public sealed record RespuestaAsistente(
    string Respuesta,
    IReadOnlyList<string> HerramientasUsadas,
    string ProveedorUsado,
    string ModeloUsado);

public sealed class AsistenteNoDisponibleException(Exception innerException)
    : Exception("Los proveedores del asistente no están disponibles temporalmente.", innerException);
