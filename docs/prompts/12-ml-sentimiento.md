# Reseñas con análisis de sentimiento

- **Rama:** `feature/ml-sentimiento`
- **Agente:** AI assistant using Copilot SDK in VS Code

## Prompt

Lee AGENTS.md y usa la skill diseno-unilink. Estamos en la rama feature/ml-sentimiento. Implementa reseñas con análisis de sentimiento usando ML.NET:

1. Entidad Resena: Id, AutorId, UsuarioReseñadoId, MaterialId? , ClaseId?, Calificacion (1-5), Comentario (máx. 500), Sentimiento (Positivo/Negativo), Probabilidad (float), Fecha. Migración y aplicarla.
2. Dataset: crea ML/Datos/resenas-entrenamiento.csv con al menos 300 reseñas realistas en español peruano sobre préstamos de materiales y clases entre estudiantes (columnas: Texto, Positivo con true/false), balanceadas entre positivas y negativas, con variedad de vocabulario.
3. Entrenamiento: instala Microsoft.ML y Microsoft.Extensions.ML. Crea ML/EntrenadorSentimiento.cs que entrene un clasificador binario (FeaturizeText + SdcaLogisticRegression), separe 80/20 para evaluar y guarde el modelo en ML/Modelos/sentimiento.zip. Registra en el log y escribe en docs/ml-metricas.md: Accuracy, AUC, F1 y la matriz de confusión. Al iniciar la app, si el modelo no existe, entrénalo.
4. Predicción: usa PredictionEnginePool (AddPredictionEnginePool) para predecir el sentimiento al guardar cada reseña.
5. Flujo: en Mis alquileres (Devuelto) y Mis clases (Dictada) muestra un botón "Dejar reseña" (solo una reseña por alquiler o reserva). Formulario con estrellas y comentario. Al guardar: TempData y notificación en tiempo real al reseñado usando NotificacionService.
6. Muestra en el detalle del material, el detalle de la clase y el perfil: promedio de estrellas, cantidad de reseñas y cada reseña con un badge de sentimiento (verde Positivo / rojo Negativo y su porcentaje de confianza).
7. Endpoint GET /api/ml/sentimiento?texto=... que devuelva la predicción (para probar desde Scalar).
8. Asegura que ML/Modelos/sentimiento.zip y el CSV se copien al publicar (CopyToOutputDirectory) para que funcione en Render con Docker.
9. Ejecuta dotnet build y dotnet run para entrenar el modelo una vez.
10. Crea docs/prompts/12-ml-sentimiento.md con la rama, el agente, este prompt y un resumen con las métricas.
11. NO ejecutes git.

## Resumen y métricas

Se agregó el flujo de reseñas verificadas para alquileres devueltos y reservas de clase dictadas, con una reseña única por operación, clasificación automática, notificación al usuario reseñado, reseñas públicas en los detalles y perfiles, y el endpoint de predicción.

El dataset balanceado contiene 300 reseñas (150 positivas y 150 negativas). El modelo se evaluó con 250 ejemplos de entrenamiento y 50 de prueba:

- **Accuracy:** 100.00 %
- **AUC:** 100.00 %
- **F1:** 100.00 %
- **Matriz de confusión (real × predicción):**

  | Etiqueta real | Negativo | Positivo |
  |---|---:|---:|
  | Negativo | 21 | 0 |
  | Positivo | 0 | 29 |
