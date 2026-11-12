using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;

namespace UniLink.Services;

public sealed class RecordatoriosAlquilerBackgroundService(
    IServiceScopeFactory scopeFactory,
    ColaCorreos colaCorreos,
    IConfiguration configuration,
    ILogger<RecordatoriosAlquilerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!colaCorreos.Habilitada)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublicarRecordatoriosAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No se pudieron revisar los alquileres que vencen mañana.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task PublicarRecordatoriosAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UniLinkDbContext>();
        var fechaManana = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var alquileres = await db.Alquileres.AsNoTracking()
            .Include(alquiler => alquiler.Material)
            .Include(alquiler => alquiler.Solicitante)
            .Where(alquiler => alquiler.Estado == EstadoSolicitud.Activo && alquiler.FechaFin == fechaManana)
            .ToListAsync(cancellationToken);

        foreach (var alquiler in alquileres)
        {
            var baseUrl = (
                configuration["App:BaseUrl"]
                ?? Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL"))?.TrimEnd('/');
            var rutaDetalle = $"/Materiales/Detalle/{alquiler.MaterialId}";
            var url = Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                ? new Uri(uri, rutaDetalle).AbsoluteUri
                : "#";
            if (url == "#")
            {
                logger.LogWarning("Configura App:BaseUrl para incluir enlaces absolutos en los recordatorios de alquiler.");
            }

            if (string.IsNullOrWhiteSpace(alquiler.Solicitante.Email))
            {
                logger.LogWarning("Se omitió el recordatorio del alquiler {AlquilerId}: el solicitante no tiene correo.", alquiler.Id);
                continue;
            }

            await colaCorreos.PublicarAsync(
                new CorreoMensaje(
                    alquiler.Solicitante.Email ?? string.Empty,
                    "Tu alquiler vence mañana",
                    "alquiler-vence-manana",
                    new Dictionary<string, string>
                    {
                        ["Titulo"] = "Tu alquiler vence mañana",
                        ["Mensaje"] = $"Recuerda devolver {alquiler.Material.Nombre} mañana, {alquiler.FechaFin:dd/MM/yyyy}.",
                        ["BotonTexto"] = "Ver material",
                        ["BotonUrl"] = url
                    }),
                cancellationToken);
        }
    }
}
