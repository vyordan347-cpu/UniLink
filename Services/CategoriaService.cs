using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;

namespace UniLink.Services;

public interface ICategoriaService
{
    Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync(CancellationToken cancellationToken = default);
}

public sealed class CategoriaService(
    UniLinkDbContext db,
    IDistributedCache cache,
    ILogger<CategoriaService> logger) : ICategoriaService
{
    private const string ClaveCache = "categorias";
    private static readonly DistributedCacheEntryOptions OpcionesCache = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    public async Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        var categoriasEnCache = await cache.GetStringAsync(ClaveCache, cancellationToken);
        if (categoriasEnCache is not null)
        {
            var datosCache = JsonSerializer.Deserialize<List<CategoriaCache>>(categoriasEnCache)
                ?? throw new InvalidOperationException("La caché de categorías contiene datos no válidos.");

            logger.LogInformation("Categorías obtenidas de caché.");
            return datosCache.Select(ConvertirCategoria).ToArray();
        }

        var datos = await db.Categorias
            .AsNoTracking()
            .OrderBy(categoria => categoria.Nombre)
            .Select(categoria => new CategoriaCache(categoria.Id, categoria.Nombre))
            .ToListAsync(cancellationToken);

        await cache.SetStringAsync(
            ClaveCache,
            JsonSerializer.Serialize(datos),
            OpcionesCache,
            cancellationToken);

        logger.LogInformation("Categorías obtenidas de base de datos.");
        return datos.Select(ConvertirCategoria).ToArray();
    }

    private static Categoria ConvertirCategoria(CategoriaCache categoria) =>
        new()
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre
        };

    private sealed record CategoriaCache(int Id, string Nombre);
}
