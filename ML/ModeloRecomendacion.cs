using Microsoft.AspNetCore.Hosting;
using Microsoft.ML;

namespace UniLink.ML;

public sealed class ModeloRecomendacion
{
    private readonly object _bloqueo = new();
    private readonly MLContext _contexto = new(seed: 42);
    private readonly string _rutaModelo;
    private readonly ILogger<ModeloRecomendacion> _logger;
    private ITransformer? _modelo;

    public ModeloRecomendacion(
        IWebHostEnvironment entorno,
        ILogger<ModeloRecomendacion> logger)
    {
        _rutaModelo = Path.Combine(entorno.ContentRootPath, "ML", "Modelos", "recomendacion.zip");
        _logger = logger;

        if (!File.Exists(_rutaModelo))
        {
            return;
        }

        try
        {
            _modelo = _contexto.Model.Load(_rutaModelo, out _);
            _logger.LogInformation("Se cargó el modelo de recomendaciones desde {RutaModelo}.", _rutaModelo);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "No se pudo cargar el modelo de recomendaciones desde {RutaModelo}; se usará el fallback.",
                _rutaModelo);
        }
    }

    public bool PuedePredecir
    {
        get
        {
            lock (_bloqueo)
            {
                return _modelo is not null && File.Exists(_rutaModelo);
            }
        }
    }

    public void Actualizar(ITransformer modelo)
    {
        lock (_bloqueo)
        {
            _modelo = modelo;
        }
    }

    public IReadOnlyDictionary<string, float> Predecir(
        string usuarioId,
        IReadOnlyCollection<string> items)
    {
        lock (_bloqueo)
        {
            if (_modelo is null || !File.Exists(_rutaModelo))
            {
                throw new InvalidOperationException("El modelo de recomendaciones no está cargado o el archivo ya no existe.");
            }

            var datos = _contexto.Data.LoadFromEnumerable(items.Select(item => new DatosInteraccionRecomendacion
            {
                UsuarioId = usuarioId,
                ItemId = item
            }));
            var predicciones = _modelo.Transform(datos);
            return _contexto.Data
                .CreateEnumerable<PrediccionRecomendacion>(predicciones, reuseRowObject: false)
                .Where(prediccion => float.IsFinite(prediccion.Puntaje))
                .GroupBy(prediccion => prediccion.ItemId)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.First().Puntaje);
        }
    }
}
