# Búsqueda con Algolia

## Prompt

Lee AGENTS.md y usa la skill diseno-unilink. Rama feature/algolia-busqueda. Integra la API de Algolia (paquete Algolia.Search, versión compatible con .NET 10) para buscar materiales y clases:
1. Configuración desde Algolia:AppId, Algolia:SearchKey y Algolia:AdminKey (nunca en el código). Si no están configuradas, la app funciona igual usando PostgreSQL.
2. Services/BusquedaService con dos índices: "unilink_materiales" y "unilink_clases". Indexa solo datos públicos: objectID (id), nombre o curso, descripción, carrera, categoría, precio, esSolidario y disponible. Configura los atributos buscables (nombre/curso, descripción, carrera, categoría) y los filtros (carrera, esSolidario, disponible).
3. Sincronización: al crear, editar o eliminar un material o una clase, actualiza el índice (sin bloquear la acción si Algolia falla; solo registra el error). Endpoint POST /api/busqueda/reindexar ([Authorize]) que sube todos los materiales y clases. Al iniciar la app, si el índice está vacío, indexa todo una vez.
4. Endpoint GET /api/busqueda?q=&tipo=materiales|clases&soloGratuitos= : consulta Algolia (tolerante a errores de escritura), toma los ids y trae los datos completos desde PostgreSQL. Si Algolia no está configurado o falla, busca en PostgreSQL con ILIKE. La respuesta incluye "motor": "algolia" o "postgresql".
5. En Explorar agrega un buscador grande "Busca materiales, cursos o tutores..." con resultados mientras se escribe (debounce de 300 ms) usando ese endpoint, y una pequeña etiqueta "Búsqueda con Algolia" cuando se use ese motor.
6. Documenta los endpoints con comentarios /// para que aparezcan en /scalar.
7. Ejecuta dotnet build. Crea docs/prompts/19-algolia.md con este prompt y un resumen. No ejecutes git ni muestres secretos.

## Resumen

- Se agregó Algolia.Search 7.54.0, compatible con .NET 10 mediante su destino .NET Standard.
- Se creó `BusquedaService` para los índices `unilink_materiales` y `unilink_clases`, con ajustes de campos buscables y filtrables, sincronización individual, reindexación completa e inicialización de índices vacíos.
- Se crearon `GET /api/busqueda` y `POST /api/busqueda/reindexar`. Las consultas Algolia recuperan los registros actuales desde PostgreSQL; ante falta de configuración o fallos de Algolia, la búsqueda usa `ILIKE`.
- Publicar, editar y eliminar materiales o clases sincroniza el índice. También se sincroniza `disponible` cuando un alquiler se marca como entregado o devuelto. Las fallas de sincronización quedan en el log y no deshacen el cambio de PostgreSQL.
- Se agregó el buscador responsive a las vistas de materiales y clases, con espera de 300 ms, resultados accesibles y etiqueta de motor para Algolia.
- La configuración se lee de `Algolia:AppId`, `Algolia:SearchKey` y `Algolia:AdminKey`, sin almacenar credenciales en el repositorio.
