using System.Text.Json.Serialization;
using Algolia.Search.Clients;
using Algolia.Search.Exceptions;
using Algolia.Search.Http;
using Algolia.Search.Models.Search;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;

namespace UniLink.Services;

public sealed class BusquedaService
{
    public const string IndiceMateriales = "unilink_materiales";
    public const string IndiceClases = "unilink_clases";

    private static readonly string[] AtributosBuscables =
    [
        "nombre,curso",
        "descripcion",
        "carrera",
        "categoria"
    ];

    private static readonly string[] AtributosFiltrables =
    [
        "filterOnly(carrera)",
        "filterOnly(esSolidario)",
        "filterOnly(disponible)"
    ];

    private readonly UniLinkDbContext _db;
    private readonly ILogger<BusquedaService> _logger;
    private readonly string? _appId;
    private readonly string? _searchKey;
    private readonly string? _adminKey;

    public BusquedaService(
        UniLinkDbContext db,
        IConfiguration configuration,
        ILogger<BusquedaService> logger)
    {
        _db = db;
        _logger = logger;
        _appId = configuration["Algolia:AppId"];
        _searchKey = configuration["Algolia:SearchKey"];
        _adminKey = configuration["Algolia:AdminKey"];
    }

    public bool PuedeBuscar =>
        !string.IsNullOrWhiteSpace(_appId) && !string.IsNullOrWhiteSpace(_searchKey);

    public bool PuedeIndexar =>
        !string.IsNullOrWhiteSpace(_appId) && !string.IsNullOrWhiteSpace(_adminKey);

    public async Task<IReadOnlyList<string>?> BuscarIdsAsync(
        string indice,
        string consulta,
        bool soloGratuitos,
        CancellationToken cancellationToken)
    {
        if (!PuedeBuscar)
        {
            return null;
        }

        try
        {
            var cliente = CrearCliente(_searchKey!);
            var respuesta = await cliente.SearchSingleIndexAsync<DocumentoBusqueda>(
                indice,
                new SearchParams(new SearchParamsObject
                {
                    Query = consulta,
                    HitsPerPage = 100,
                    Filters = soloGratuitos ? "esSolidario:true" : null
                }),
                cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            return respuesta.Hits
                .Select(documento => documento.ObjectID)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Falló la búsqueda en el índice Algolia {Indice}; se usará PostgreSQL.", indice);
            return null;
        }
    }

    public async Task SincronizarMaterialAsync(int materialId, CancellationToken cancellationToken)
    {
        if (!PuedeIndexar)
        {
            return;
        }

        try
        {
            var material = await _db.Materiales.AsNoTracking()
                .Include(item => item.Categoria)
                .FirstOrDefaultAsync(item => item.Id == materialId, cancellationToken);

            if (material is null)
            {
                await CrearCliente(_adminKey!).DeleteObjectAsync(
                    IndiceMateriales,
                    materialId.ToString(),
                    cancellationToken: cancellationToken);
                return;
            }

            await CrearCliente(_adminKey!).SaveObjectAsync(IndiceMateriales, new Dictionary<string, object?>
            {
                ["objectID"] = material.Id.ToString(),
                ["nombre"] = material.Nombre,
                ["descripcion"] = material.Descripcion,
                ["carrera"] = material.Carrera,
                ["categoria"] = material.Categoria.Nombre,
                ["precio"] = material.PrecioDia,
                ["esSolidario"] = material.EsSolidario,
                ["disponible"] = material.Disponible
            },
                cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo sincronizar el material {MaterialId} con Algolia.", materialId);
        }
    }

    public async Task SincronizarClaseAsync(int claseId, CancellationToken cancellationToken)
    {
        if (!PuedeIndexar)
        {
            return;
        }

        try
        {
            var clase = await _db.Clases.AsNoTracking()
                .Include(item => item.Tutor)
                .FirstOrDefaultAsync(item => item.Id == claseId, cancellationToken);

            if (clase is null)
            {
                await CrearCliente(_adminKey!).DeleteObjectAsync(
                    IndiceClases,
                    claseId.ToString(),
                    cancellationToken: cancellationToken);
                return;
            }

            await CrearCliente(_adminKey!).SaveObjectAsync(IndiceClases, new Dictionary<string, object?>
            {
                ["objectID"] = clase.Id.ToString(),
                ["nombre"] = clase.Tutor.Nombre,
                ["curso"] = clase.Curso,
                ["descripcion"] = clase.Descripcion,
                ["carrera"] = clase.Tutor.Carrera,
                ["categoria"] = string.Empty,
                ["precio"] = clase.PrecioHora,
                ["esSolidario"] = clase.EsSolidaria,
                ["disponible"] = true
            },
                cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo sincronizar la clase {ClaseId} con Algolia.", claseId);
        }
    }

    public async Task ReindexarTodoAsync(CancellationToken cancellationToken)
    {
        if (!PuedeIndexar)
        {
            throw new InvalidOperationException("Algolia requiere Algolia:AppId y Algolia:AdminKey para reindexar.");
        }

        var materiales = await ObtenerDocumentosMaterialesAsync(cancellationToken);
        var clases = await ObtenerDocumentosClasesAsync(cancellationToken);
        var cliente = CrearCliente(_adminKey!);

        var estadoMateriales = await ObtenerEstadoIndiceAsync(cliente, IndiceMateriales, cancellationToken);
        var estadoClases = await ObtenerEstadoIndiceAsync(cliente, IndiceClases, cancellationToken);
        await IndexarIndiceAsync(cliente, IndiceMateriales, materiales, estadoMateriales, cancellationToken);
        await IndexarIndiceAsync(cliente, IndiceClases, clases, estadoClases, cancellationToken);
    }

    public async Task InicializarIndicesVaciosAsync(CancellationToken cancellationToken)
    {
        if (!PuedeIndexar)
        {
            return;
        }

        try
        {
            var cliente = CrearCliente(_adminKey!);
            var estadoMateriales = await ObtenerEstadoIndiceAsync(cliente, IndiceMateriales, cancellationToken);
            var estadoClases = await ObtenerEstadoIndiceAsync(cliente, IndiceClases, cancellationToken);
            if (estadoMateriales != EstadoIndice.ConDatos || estadoClases != EstadoIndice.ConDatos)
            {
                var materiales = await ObtenerDocumentosMaterialesAsync(cancellationToken);
                var clases = await ObtenerDocumentosClasesAsync(cancellationToken);
                await IndexarIndiceAsync(cliente, IndiceMateriales, materiales, estadoMateriales, cancellationToken);
                await IndexarIndiceAsync(cliente, IndiceClases, clases, estadoClases, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "No se pudieron inicializar los índices Algolia; la aplicación seguirá usando PostgreSQL.");
        }
    }

    private async Task<EstadoIndice> ObtenerEstadoIndiceAsync(
        SearchClient cliente,
        string indice,
        CancellationToken cancellationToken)
    {
        try
        {
            var respuesta = await cliente.SearchSingleIndexAsync<DocumentoBusqueda>(
                indice,
                new SearchParams(new SearchParamsObject
                {
                    Query = string.Empty,
                    HitsPerPage = 1
                }),
                cancellationToken: cancellationToken);

            return respuesta.NbHits == 0 ? EstadoIndice.Vacio : EstadoIndice.ConDatos;
        }
        catch (AlgoliaApiException exception) when (exception.HttpErrorCode == 404)
        {
            return EstadoIndice.Inexistente;
        }
    }

    private async Task IndexarIndiceAsync(
        SearchClient cliente,
        string indice,
        List<Dictionary<string, object?>> documentos,
        EstadoIndice estado,
        CancellationToken cancellationToken)
    {
        if (documentos.Count == 0 && estado == EstadoIndice.Inexistente)
        {
            const string idTemporal = "_unilink_initializing_";
            var tareaCreacion = await cliente.SaveObjectAsync(
                indice,
                new Dictionary<string, object?> { ["objectID"] = idTemporal },
                cancellationToken: cancellationToken);
            await cliente.WaitForTaskAsync(indice, tareaCreacion.TaskID, 100, null, null, cancellationToken);

            var tareaConfiguracion = await ConfigurarIndiceAsync(cliente, indice, cancellationToken);
            await cliente.WaitForTaskAsync(indice, tareaConfiguracion.TaskID, 100, null, null, cancellationToken);

            var tareaLimpieza = await cliente.DeleteObjectAsync(indice, idTemporal, cancellationToken: cancellationToken);
            await cliente.WaitForTaskAsync(indice, tareaLimpieza.TaskID, 100, null, null, cancellationToken);
            _logger.LogInformation("Índice {Indice} creado e indexado con {Cantidad} registros.", indice, 0);
            return;
        }

        if (documentos.Count > 0)
        {
            await cliente.SaveObjectsAsync(indice, documentos, cancellationToken: cancellationToken);
        }

        var tareaAjustes = await ConfigurarIndiceAsync(cliente, indice, cancellationToken);
        await cliente.WaitForTaskAsync(indice, tareaAjustes.TaskID, 100, null, null, cancellationToken);

        if (estado == EstadoIndice.Inexistente)
        {
            _logger.LogInformation(
                "Índice {Indice} creado e indexado con {Cantidad} registros.",
                indice,
                documentos.Count);
        }
    }

    private static Task<Algolia.Search.Models.Search.UpdatedAtResponse> ConfigurarIndiceAsync(
        SearchClient cliente,
        string indice,
        CancellationToken cancellationToken)
    {
        return cliente.SetSettingsAsync(indice, new IndexSettings
        {
            SearchableAttributes = AtributosBuscables.ToList(),
            AttributesForFaceting = AtributosFiltrables.ToList()
        }, cancellationToken: cancellationToken);
    }

    private async Task<List<Dictionary<string, object?>>> ObtenerDocumentosMaterialesAsync(
        CancellationToken cancellationToken)
    {
        var materiales = await _db.Materiales.AsNoTracking()
            .Include(item => item.Categoria)
            .ToListAsync(cancellationToken);

        return materiales.Select(item => new Dictionary<string, object?>
        {
            ["objectID"] = item.Id.ToString(),
            ["nombre"] = item.Nombre,
            ["descripcion"] = item.Descripcion,
            ["carrera"] = item.Carrera,
            ["categoria"] = item.Categoria.Nombre,
            ["precio"] = item.PrecioDia,
            ["esSolidario"] = item.EsSolidario,
            ["disponible"] = item.Disponible
        })
            .ToList();
    }

    private async Task<List<Dictionary<string, object?>>> ObtenerDocumentosClasesAsync(
        CancellationToken cancellationToken)
    {
        var clases = await _db.Clases.AsNoTracking()
            .Include(item => item.Tutor)
            .ToListAsync(cancellationToken);

        return clases.Select(item => new Dictionary<string, object?>
        {
            ["objectID"] = item.Id.ToString(),
            ["nombre"] = item.Tutor.Nombre,
            ["curso"] = item.Curso,
            ["descripcion"] = item.Descripcion,
            ["carrera"] = item.Tutor.Carrera,
            ["categoria"] = string.Empty,
            ["precio"] = item.PrecioHora,
            ["esSolidario"] = item.EsSolidaria,
            ["disponible"] = true
        })
            .ToList();
    }

    private SearchClient CrearCliente(string apiKey) =>
        new(new SearchConfig(_appId!, apiKey));

    private sealed class DocumentoBusqueda
    {
        [JsonPropertyName("objectID")]
        public string ObjectID { get; set; } = string.Empty;
    }

    private enum EstadoIndice
    {
        Inexistente,
        Vacio,
        ConDatos
    }
}
