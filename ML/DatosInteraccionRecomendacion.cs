using Microsoft.ML.Data;

namespace UniLink.ML;

public sealed class DatosInteraccionRecomendacion
{
    [LoadColumn(0)]
    public string UsuarioId { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string ItemId { get; set; } = string.Empty;

    [LoadColumn(2)]
    public float Puntaje { get; set; }
}

public sealed class PrediccionRecomendacion
{
    [ColumnName("Score")]
    public float Puntaje { get; set; }

    public string ItemId { get; set; } = string.Empty;
}
