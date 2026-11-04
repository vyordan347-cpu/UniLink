# Recomendaciones con ML.NET

- **Rama:** `feature/ml-recomendacion`
- **Agente:** AI assistant using Copilot SDK in VS Code

## Prompt

Lee AGENTS.md y usa la skill diseno-unilink. Estamos en la rama feature/ml-recomendacion. Implementa recomendaciones con ML.NET:

1. Instala Microsoft.ML.Recommender.
2. Dataset: crea ML/Datos/interacciones-semilla.csv con al menos 500 interacciones sintéticas (UsuarioId, ItemId, Puntaje 1-5) que simulen estudiantes de Odontología, Medicina e Ingeniería con preferencias coherentes por su carrera. Al entrenar, combina este CSV con las interacciones reales de la base de datos (alquileres y reservas, usando como puntaje la calificación de la reseña o 4 si no hay).
3. ML/EntrenadorRecomendacion.cs: MatrixFactorization (MapValueToKey para usuario e ítem), evaluación 80/20 con RMSE y R², modelo guardado en ML/Modelos/recomendacion.zip y métricas agregadas a docs/ml-metricas.md.
4. Services/RecomendacionService: para un usuario, predice el puntaje de los materiales y clases disponibles que no son suyos ni ya alquilados, y devuelve el top 6. Si el usuario no tiene historial (arranque en frío), recomienda los más populares de su carrera. Cachea el resultado por usuario en Redis durante 30 minutos.
5. Reentrenamiento: un BackgroundService que reentrene el modelo al iniciar y luego cada 24 horas.
6. En el Panel: sección "Te puede interesar ✨" con tarjetas (según la skill) y la etiqueta "Recomendado para ti".
7. Endpoint GET /api/ml/recomendaciones (autenticado) que devuelva las recomendaciones del usuario actual.
8. Asegura que los CSV y modelos se copien al publicar.
9. Ejecuta dotnet build y dotnet run para entrenar una vez.
10. Crea docs/prompts/13-ml-recomendacion.md con la rama, el agente, este prompt y un resumen con las métricas.
11. NO ejecutes git.

## Resumen y métricas

Se agregó el paquete Microsoft.ML.Recommender 0.23.1, un dataset sintético de 900 interacciones con afinidades por carrera, el entrenamiento MatrixFactorization que combina alquileres y reservas con calificación de reseña o 4 por defecto, y la recomendación de materiales y clases con filtro de propiedad e historial, arranque en frío por popularidad y caché de 30 minutos. También se conectó el reentrenamiento cada 24 horas, la sección visual del Panel y el endpoint autenticado.

- **Build:** correcto, 0 advertencias y 0 errores.
- **dotnet run:** no pudo completar el entrenamiento; falta configurar `ConnectionStrings:DefaultConnection` mediante user-secrets o una variable de entorno. No se inventan ni se guardan credenciales.
- **RMSE / R²:** pendientes de ejecución con una base de datos configurada; el entrenador los escribirá en `docs/ml-metricas.md`.
