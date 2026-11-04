using Microsoft.ML;

namespace UniLink.ML;

public sealed class ModeloRecomendacion
{
    private readonly object _bloqueo = new();
    private readonly MLContext _contexto = new(seed: 42);
    private ITransformer? _modelo;

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
            if (_modelo is null)
            {
                throw new InvalidOperationException("El modelo de recomendaciones todavía no está entrenado.");
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
