using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UniLink.Models;

namespace UniLink.Services;

public sealed class ConsumidorCorreos(
    ColaCorreos colaCorreos,
    IServiceScopeFactory scopeFactory,
    ILogger<ConsumidorCorreos> logger) : BackgroundService
{
    private const int MaximoReintentos = 3;
    private const string CabeceraReintentos = "x-unilink-retry";
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!colaCorreos.Habilitada)
        {
            logger.LogWarning("La cola de correos no está habilitada porque falta CloudAMQP:Url o Resend:ApiKey.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var canal = await colaCorreos.AbrirCanalAsync(stoppingToken);
                if (canal is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                await canal.BasicQosAsync(0, 1, false, stoppingToken);
                var consumidor = new AsyncEventingBasicConsumer(canal);
                consumidor.ReceivedAsync += (_, mensaje) => ProcesarAsync(canal, mensaje, stoppingToken);

                await canal.BasicConsumeAsync(
                    queue: ColaCorreos.NombreCola,
                    autoAck: false,
                    consumer: consumidor,
                    cancellationToken: stoppingToken);

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "El consumidor de correos perdió la conexión con CloudAMQP; volverá a intentar.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcesarAsync(
        IChannel canal,
        BasicDeliverEventArgs entrega,
        CancellationToken cancellationToken)
    {
        try
        {
            var correo = JsonSerializer.Deserialize<CorreoMensaje>(entrega.Body.Span, OpcionesJson)
                ?? throw new JsonException("El mensaje no contiene un correo válido.");

            await using var scope = scopeFactory.CreateAsyncScope();
            var remitente = scope.ServiceProvider.GetRequiredService<EnvioCorreoResend>();
            await remitente.EnviarAsync(correo, cancellationToken);
            await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var reintentos = LeerReintentos(entrega.BasicProperties.Headers);
            if (reintentos < MaximoReintentos)
            {
                var siguienteReintento = reintentos + 1;
                logger.LogWarning(
                    exception,
                    "Falló el envío de un correo. Se programará el reintento {Reintento} de {MaximoReintentos}.",
                    siguienteReintento,
                    MaximoReintentos);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, siguienteReintento)), cancellationToken);
                var propiedades = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = entrega.BasicProperties.MessageId,
                    Headers = new Dictionary<string, object?>
                    {
                        [CabeceraReintentos] = siguienteReintento
                    }
                };
                await canal.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: ColaCorreos.NombreCola,
                    mandatory: true,
                    basicProperties: propiedades,
                    body: entrega.Body,
                    cancellationToken);
                await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, cancellationToken);
                return;
            }

            logger.LogError(
                exception,
                "Se descartó un correo después de {MaximoReintentos} reintentos.",
                MaximoReintentos);
            await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, cancellationToken);
        }
    }

    private static int LeerReintentos(IDictionary<string, object?>? encabezados)
    {
        if (encabezados is null || !encabezados.TryGetValue(CabeceraReintentos, out var valor))
        {
            return 0;
        }

        if (valor is byte[] bytes
            && int.TryParse(Encoding.UTF8.GetString(bytes), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero))
        {
            return numero;
        }

        return int.TryParse(
            Convert.ToString(valor, CultureInfo.InvariantCulture),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out numero)
            ? numero
            : 0;
    }
}
