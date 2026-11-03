using Microsoft.ML.Data;

namespace UniLink.ML;

public class DatosSentimiento
{
    [LoadColumn(0)]
    public string Texto { get; set; } = string.Empty;

    [LoadColumn(1)]
    public bool Positivo { get; set; }
}

public class PrediccionSentimiento
{
    [ColumnName("PredictedLabel")]
    public bool Positivo { get; set; }

    public float Probability { get; set; }

    public float Confianza => Positivo ? Probability : 1 - Probability;
}
