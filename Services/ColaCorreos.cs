using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using UniLink.Models;

namespace UniLink.Services;

public sealed class ColaCorreos : IAsyncDisposable
{
    public const string NombreCola = "unilink.correos";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);
    private readonly string? _url;
    private readonly ILogger<ColaCorreos> _logger;
    private readonly SemaphoreSlim _candadoConexion = new(1, 1);
    private IConnection? _conexion;

    public ColaCorreos(IConfiguration configuration, ILogger<ColaCorreos> logger)
    {
        _url = configuration["CloudAMQP:Url"];
        _logger = logger;
        CloudAMQPConfigurado = !string.IsNullOrWhiteSpace(_url);
        Habilitada = !string.IsNullOrWhiteSpace(_url)
            && !string.IsNullOrWhiteSpace(configuration["Resend:ApiKey"]);
    }

    public bool CloudAMQPConfigurado { get; }
    public bool Habilitada { get; }

    public async Task<bool> PublicarAsync(CorreoMensaje correo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(correo);
        if (!CloudAMQPConfigurado)
        {
            return false;
        }

        try
        {
            var conexion = await ObtenerConexionAsync(cancellationToken);
            if (conexion is null)
            {
                return false;
            }

            await using var canal = await conexion.CreateChannelAsync(cancellationToken: cancellationToken);
            await DeclararColaAsync(canal, cancellationToken);

            var propiedades = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString("N")
            };
            var cuerpo = JsonSerializer.SerializeToUtf8Bytes(correo, OpcionesJson);
            await canal.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: NombreCola,
                mandatory: true,
                basicProperties: propiedades,
                body: cuerpo,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "No se pudo publicar un correo en la cola {Cola}.", NombreCola);
            return false;
        }
    }

    public async Task<IChannel?> AbrirCanalAsync(CancellationToken cancellationToken)
    {
        var conexion = await ObtenerConexionAsync(cancellationToken);
        if (conexion is null)
        {
            return null;
        }

        var canal = await conexion.CreateChannelAsync(cancellationToken: cancellationToken);
        try
        {
            await DeclararColaAsync(canal, cancellationToken);
            return canal;
        }
        catch
        {
            await canal.DisposeAsync();
            throw;
        }
    }

    public async Task<bool> VerificarConexionAsync(CancellationToken cancellationToken)
    {
        if (!Habilitada)
        {
            return false;
        }

        try
        {
            await using var canal = await AbrirCanalAsync(cancellationToken);
            return canal is not null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "No se pudo comprobar la conexión con CloudAMQP.");
            return false;
        }
    }

    private async Task<IConnection?> ObtenerConexionAsync(CancellationToken cancellationToken)
    {
        if (!CloudAMQPConfigurado)
        {
            return null;
        }

        await _candadoConexion.WaitAsync(cancellationToken);
        try
        {
            if (_conexion?.IsOpen == true)
            {
                return _conexion;
            }

            if (_conexion is not null)
            {
                await _conexion.DisposeAsync();
                _conexion = null;
            }

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_url!, UriKind.Absolute),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
            _conexion = await factory.CreateConnectionAsync(cancellationToken);
            return _conexion;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "No se pudo conectar con CloudAMQP.");
            return null;
        }
        finally
        {
            _candadoConexion.Release();
        }
    }

    private static async Task DeclararColaAsync(IChannel canal, CancellationToken cancellationToken)
    {
        await canal.QueueDeclareAsync(
            queue: NombreCola,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_conexion is not null)
        {
            await _conexion.DisposeAsync();
        }

        _candadoConexion.Dispose();
    }
}
