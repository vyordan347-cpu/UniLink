using System.Globalization;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace UniLink.ML;

public sealed class EntrenadorSentimiento(ILogger<EntrenadorSentimiento> logger)
{
    public (string RutaModelo, string Informe) Entrenar(string directorioContenido)
    {
        var rutaDatos = Path.Combine(directorioContenido, "ML", "Datos", "resenas-entrenamiento.csv");
        var rutaModelo = Path.Combine(directorioContenido, "ML", "Modelos", "sentimiento.zip");
        if (!File.Exists(rutaDatos))
        {
            throw new FileNotFoundException("No se encontró el dataset de entrenamiento de reseñas.", rutaDatos);
        }

        var contexto = new MLContext(seed: 42);
        var datos = contexto.Data.LoadFromTextFile<DatosSentimiento>(
            rutaDatos,
            separatorChar: ',',
            hasHeader: true,
            allowQuoting: true);
        var particion = contexto.Data.TrainTestSplit(datos, testFraction: 0.2, seed: 42);
        var pipeline = contexto.Transforms.Text.FeaturizeText(
                outputColumnName: "Caracteristicas",
                inputColumnName: nameof(DatosSentimiento.Texto))
            .Append(contexto.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(DatosSentimiento.Positivo),
                featureColumnName: "Caracteristicas"));

        var modelo = pipeline.Fit(particion.TrainSet);
        var predicciones = modelo.Transform(particion.TestSet);
        var metricas = contexto.BinaryClassification.Evaluate(
            predicciones,
            labelColumnName: nameof(DatosSentimiento.Positivo));

        Directory.CreateDirectory(Path.GetDirectoryName(rutaModelo)!);
        contexto.Model.Save(modelo, particion.TrainSet.Schema, rutaModelo);

        var matriz = metricas.ConfusionMatrix.Counts;
        var filasEntrenamiento = contexto.Data
            .CreateEnumerable<DatosSentimiento>(particion.TrainSet, reuseRowObject: false)
            .LongCount();
        var filasPrueba = contexto.Data
            .CreateEnumerable<DatosSentimiento>(particion.TestSet, reuseRowObject: false)
            .LongCount();
        var informe = new StringBuilder()
            .AppendLine("# Métricas del clasificador de sentimiento")
            .AppendLine()
            .AppendLine("Evaluación sobre una partición de prueba del 20 % del dataset (semilla fija: 42).")
            .AppendLine()
            .AppendLine($"- Reseñas de entrenamiento: {filasEntrenamiento}")
            .AppendLine($"- Reseñas de prueba: {filasPrueba}")
            .AppendLine($"- Accuracy: {metricas.Accuracy.ToString("P2", CultureInfo.InvariantCulture)}")
            .AppendLine($"- AUC: {metricas.AreaUnderRocCurve.ToString("P2", CultureInfo.InvariantCulture)}")
            .AppendLine($"- F1: {metricas.F1Score.ToString("P2", CultureInfo.InvariantCulture)}")
            .AppendLine()
            .AppendLine("## Matriz de confusión")
            .AppendLine()
            .AppendLine("| Etiqueta real \\ Predicción | Negativo | Positivo |")
            .AppendLine("|---|---:|---:|");

        for (var fila = 0; fila < matriz.Count; fila++)
        {
            informe.Append("| ")
                .Append(fila == 0 ? "Negativo" : "Positivo")
                .Append(" | ")
                .Append(matriz[fila][0].ToString(CultureInfo.InvariantCulture))
                .Append(" | ")
                .Append(matriz[fila][1].ToString(CultureInfo.InvariantCulture))
                .AppendLine(" |");
        }

        var contenidoInforme = informe.ToString();
        logger.LogInformation(
            "Clasificador de sentimiento entrenado. Accuracy: {Accuracy:P2}; AUC: {Auc:P2}; F1: {F1:P2}. Matriz: {Matriz}",
            metricas.Accuracy,
            metricas.AreaUnderRocCurve,
            metricas.F1Score,
            string.Join(" / ", matriz.Select(fila => string.Join(", ", fila))));

        var rutaInforme = Path.Combine(directorioContenido, "docs", "ml-metricas.md");
        Directory.CreateDirectory(Path.GetDirectoryName(rutaInforme)!);
        File.WriteAllText(rutaInforme, contenidoInforme, Encoding.UTF8);
        return (rutaModelo, contenidoInforme);
    }
}
