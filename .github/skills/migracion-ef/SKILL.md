---
name: migracion-ef
description: "Crea, revisa y aplica migraciones de Entity Framework Core con PostgreSQL en UniLink. Úsala para cambios de modelo, generar migraciones o actualizar una base local/de desarrollo de forma segura."
---

# Migraciones de EF Core en UniLink

## Cuándo usarla

- Al modificar entidades, relaciones o configuración del modelo de datos.
- Al crear una migración o actualizar una base de datos PostgreSQL local/de desarrollo.

## Procedimiento

1. Lee `AGENTS.md`, revisa `UniLink.csproj`, el `DbContext` y las migraciones existentes en `Migrations/`. Comprueba qué cambios de modelo justifican la migración.
2. Confirma que las credenciales se obtienen de `dotnet user-secrets` o variables de entorno. Nunca las agregues a `appsettings.json`, al código, a comandos registrados ni a la salida de diagnóstico.
3. Crea una migración con un nombre breve y descriptivo en español, siguiendo el estilo existente en PascalCase, por ejemplo `AgregarEstadoSolicitud`:

   ```powershell
   dotnet ef migrations add NombreDescriptivo --project UniLink.csproj --startup-project UniLink.csproj --output-dir Migrations
   ```

4. Antes de aplicar nada, inspecciona el archivo de migración generado y el snapshot del modelo. Comprueba que:
   - Las operaciones coincidan con el cambio solicitado y no haya cambios accidentales.
   - No se eliminen tablas, columnas ni datos inadvertidamente.
   - Los cambios de nombre o tipo conserven los datos cuando corresponda, mediante una migración explícita de renombrado o transformación revisada.
   - Los valores por defecto y los cambios de nulabilidad sean seguros para filas ya existentes.
5. Si la migración es incorrecta, corrige el modelo o la migración antes de aplicarla. Explica los cambios de esquema que puedan tener impacto o requerir intervención.
6. Solo después de revisar la migración, actualiza una base local/de desarrollo configurada y autorizada:

   ```powershell
   dotnet ef database update --project UniLink.csproj --startup-project UniLink.csproj
   ```

7. Valida el resultado con las pruebas pertinentes y `dotnet build`, e informa qué migración se creó, qué base se actualizó y qué verificaciones se ejecutaron.

## Salvaguardas obligatorias

- Nunca apliques migraciones a producción desde este flujo ni sin un proceso de despliegue aprobado, revisión y respaldo verificado.
- Nunca borres datos en producción. No propongas ni ejecutes una actualización productiva que elimine tablas/columnas o datos. Si el cambio parece destructivo, detente, explica el riesgo y pide una estrategia explícita de preservación y aprobación.
- No uses credenciales de producción para generar o probar migraciones.
- No ejecutes `database update` hasta revisar la migración generada.
