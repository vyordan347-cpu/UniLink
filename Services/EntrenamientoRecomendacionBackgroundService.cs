using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UniLink.ML;

namespace UniLink.Services;

public sealed class EntrenamientoRecomendacionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EntrenamientoRecomendacionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var entrenador = scope.ServiceProvider.GetRequiredService<EntrenadorRecomendacion>();
                await entrenador.EntrenarAsync(stoppingToken);
            }

            logger.LogInformation("Próximo reentrenamiento de recomendaciones en 24 horas.");
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
