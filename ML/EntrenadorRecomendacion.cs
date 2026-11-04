using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using UniLink.Data;
using UniLink.Models;

namespace UniLink.ML;

public sealed class EntrenadorRecomendacion(
    UniLinkDbContext db,
    ModeloRecomendacion modeloRecomendacion,
    IWebHostEnvironment entorno,
    ILogger<EntrenadorRecomendacion> logger)
{
    public async Task EntrenarAsync(CancellationToken cancellationToken)
    {
        var rutaDatos = Path.Combine(entorno.ContentRootPath, "ML", "Datos", "interacciones-semilla.csv");
        var rutaModelo = Path.Combine(entorno.ContentRootPath, "ML", "Modelos", "recomendacion.zip");
        if (!File.Exists(rutaDatos))
        {
            throw new FileNotFoundException("No se encontró el dataset de interacciones semilla.", rutaDatos);
        }

        var interaccionesReales = await CargarInteraccionesAsync(cancellationToken);
        var contexto = new MLContext(seed: 42);
        var datosSemilla = contexto.Data.LoadFromTextFile<DatosInteraccionRecomendacion>(
            rutaDatos,
            separatorChar: ',',
            hasHeader: true,
            allowQuoting: true);
        var interaccionesSemilla = contexto.Data
            .CreateEnumerable<DatosInteraccionRecomendacion>(datosSemilla, reuseRowObject: false)
            .ToList();
        var interacciones = interaccionesReales.Concat(interaccionesSemilla).ToList();
        if (interacciones.Count < 2)
        {
            throw new InvalidOperationException("Se necesitan al menos dos interacciones para entrenar recomendaciones.");
        }

        var datos = contexto.Data.LoadFromEnumerable(interacciones);
        var particion = contexto.Data.TrainTestSplit(datos, testFraction: 0.2, seed: 42);
        var opciones = new MatrixFactorizationTrainer.Options
        {
            MatrixColumnIndexColumnName = "UsuarioClave",
            MatrixRowIndexColumnName = "ItemClave",
            LabelColumnName = nameof(DatosInteraccionRecomendacion.Puntaje),
            NumberOfIterations = 30,
            ApproximationRank = 16
        };
        var pipeline = contexto.Transforms.Conversion.MapValueToKey(
                outputColumnName: "UsuarioClave",
                inputColumnName: nameof(DatosInteraccionRecomendacion.UsuarioId))
            .Append(contexto.Transforms.Conversion.MapValueToKey(
                outputColumnName: "ItemClave",
                inputColumnName: nameof(DatosInteraccionRecomendacion.ItemId)))
            .Append(contexto.Recommendation().Trainers.MatrixFactorization(opciones));

        var modelo = pipeline.Fit(particion.TrainSet);
        var predicciones = modelo.Transform(particion.TestSet);
        var metricas = contexto.Regression.Evaluate(
            predicciones,
            labelColumnName: nameof(DatosInteraccionRecomendacion.Puntaje));

        Directory.CreateDirectory(Path.GetDirectoryName(rutaModelo)!);
        contexto.Model.Save(modelo, particion.TrainSet.Schema, rutaModelo);
        modeloRecomendacion.Actualizar(modelo);
        await GuardarMetricasAsync(
            interaccionesReales.Count,
            interaccionesSemilla.Count,
            interacciones.Count,
            metricas.RootMeanSquaredError,
            metricas.RSquared,
            cancellationToken);

        logger.LogInformation(
            "Recomendador entrenado con {Interacciones} interacciones. RMSE: {Rmse:F4}; R²: {R2:F4}.",
            interacciones.Count,
            metricas.RootMeanSquaredError,
            metricas.RSquared);
    }

    public static string ObtenerClaveClase(string carrera) =>
        $"clase_carrera_{ObtenerCarreraNormalizada(carrera)}";

    public static string ObtenerCarreraNormalizada(string carrera)
    {
        var texto = carrera.Normalize(NormalizationForm.FormD);
        var sinAcentos = new string(texto
            .Where(caracter => CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            .ToArray())
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();

        if (sinAcentos.Contains("odont", StringComparison.Ordinal))
        {
            return "odontologia";
        }

        if (sinAcentos.Contains("medic", StringComparison.Ordinal))
        {
            return "medicina";
        }

        if (sinAcentos.Contains("ingenier", StringComparison.Ordinal))
        {
            return "ingenieria";
        }

        var clave = new string(sinAcentos.Select(caracter =>
            char.IsLetterOrDigit(caracter) ? caracter : '_').ToArray());
        return string.Join('_', clave.Split('_', StringSplitOptions.RemoveEmptyEntries));
    }

    private async Task<List<DatosInteraccionRecomendacion>> CargarInteraccionesAsync(
        CancellationToken cancellationToken)
    {
        var alquileres = await db.Alquileres
            .AsNoTracking()
            .Select(alquiler => new DatosInteraccionRecomendacion
            {
                UsuarioId = $"usuario_{alquiler.SolicitanteId}",
                ItemId = $"material_categoria_{alquiler.Material.CategoriaId}",
                Puntaje = alquiler.Resena == null ? 4 : alquiler.Resena.Calificacion
            })
            .ToListAsync(cancellationToken);
        var reservasDatos = await db.ReservasClase
            .AsNoTracking()
            .Select(reserva => new
            {
                reserva.AlumnoId,
                Carrera = reserva.Clase.Tutor.Carrera,
                Puntaje = reserva.Resena == null ? 4 : reserva.Resena.Calificacion
            })
            .ToListAsync(cancellationToken);

        var reservas = reservasDatos.Select(reserva => new DatosInteraccionRecomendacion
        {
            UsuarioId = $"usuario_{reserva.AlumnoId}",
            ItemId = ObtenerClaveClase(reserva.Carrera),
            Puntaje = reserva.Puntaje
        });
        alquileres.AddRange(reservas);
        return alquileres;
    }

    private async Task GuardarMetricasAsync(
        int cantidadInteraccionesReales,
        int cantidadInteraccionesSemilla,
        int cantidadInteracciones,
        double rmse,
        double rCuadrado,
        CancellationToken cancellationToken)
    {
        var rutaMetricas = Path.Combine(entorno.ContentRootPath, "docs", "ml-metricas.md");
        Directory.CreateDirectory(Path.GetDirectoryName(rutaMetricas)!);
        var contenido = File.Exists(rutaMetricas)
            ? await File.ReadAllTextAsync(rutaMetricas, cancellationToken)
            : "# Métricas de modelos ML.NET";
        const string encabezado = "## Recomendaciones con MatrixFactorization";
        var inicioSeccion = contenido.IndexOf(encabezado, StringComparison.Ordinal);
        if (inicioSeccion >= 0)
        {
            contenido = contenido[..inicioSeccion].TrimEnd();
        }

        var seccion = new StringBuilder()
            .AppendLine()
            .AppendLine()
            .AppendLine(encabezado)
            .AppendLine()
            .AppendLine("Evaluación sobre una partición de prueba del 20 % (semilla fija: 42).")
            .AppendLine()
            .AppendLine($"- Interacciones sintéticas: {cantidadInteraccionesSemilla}")
            .AppendLine($"- Interacciones reales (base de datos): {cantidadInteraccionesReales}")
            .AppendLine($"- Total de interacciones: {cantidadInteracciones}")
            .AppendLine($"- RMSE: {rmse.ToString("F4", CultureInfo.InvariantCulture)}")
            .AppendLine($"- R²: {rCuadrado.ToString("F4", CultureInfo.InvariantCulture)}")
            .ToString();

        await File.WriteAllTextAsync(rutaMetricas, contenido.TrimEnd() + seccion, Encoding.UTF8, cancellationToken);
    }
}
