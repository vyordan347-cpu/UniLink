using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;
using UniLink.Services;

namespace UniLink.Controllers;

[ApiController]
[Route("api/busqueda")]
public sealed class BusquedaController(
    UniLinkDbContext db,
    BusquedaService busquedaService,
    ILogger<BusquedaController> logger) : ControllerBase
{
    /// <summary>Busca materiales o clases en Algolia y obtiene sus datos completos de PostgreSQL.</summary>
    /// <param name="q">Texto de búsqueda.</param>
    /// <param name="tipo">Índice a consultar: materiales o clases.</param>
    /// <param name="soloGratuitos">Limita los resultados a publicaciones solidarias.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    /// <returns>Motor de búsqueda utilizado y resultados públicos.</returns>
    [HttpGet]
    [EndpointSummary("Busca materiales o clases")]
    [EndpointDescription("Busca por texto y devuelve resultados completos desde PostgreSQL. Usa Algolia si está disponible y PostgreSQL como alternativa.")]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? q,
        [FromQuery] string? tipo,
        [FromQuery] bool soloGratuitos,
        CancellationToken cancellationToken)
    {
        tipo = tipo?.Trim().ToLowerInvariant();
        if (tipo is not ("materiales" or "clases"))
        {
            return BadRequest(new { error = "El tipo debe ser 'materiales' o 'clases'." });
        }

        q = q?.Trim();
        if (q?.Length > 150)
        {
            return BadRequest(new { error = "La búsqueda no puede superar los 150 caracteres." });
        }

        var indice = tipo == "materiales"
            ? BusquedaService.IndiceMateriales
            : BusquedaService.IndiceClases;
        var ids = await busquedaService.BuscarIdsAsync(indice, q ?? string.Empty, soloGratuitos, cancellationToken);
        if (ids is not null)
        {
            var respuestaAlgolia = tipo == "materiales"
                ? await BuscarMaterialesPorIdsAsync(ids, soloGratuitos, cancellationToken)
                : await BuscarClasesPorIdsAsync(ids, soloGratuitos, cancellationToken);

            return Ok(new { motor = "algolia", resultados = respuestaAlgolia });
        }

        var respuestaPostgres = tipo == "materiales"
            ? await BuscarMaterialesEnPostgresAsync(q, soloGratuitos, cancellationToken)
            : await BuscarClasesEnPostgresAsync(q, soloGratuitos, cancellationToken);
        return Ok(new { motor = "postgresql", resultados = respuestaPostgres });
    }

    /// <summary>Reindexa todos los materiales y clases públicos en Algolia.</summary>
    /// <returns>Resultado de la reindexación.</returns>
    [Authorize]
    [HttpPost("reindexar")]
    [EndpointSummary("Reindexa materiales y clases")]
    [EndpointDescription("Vuelve a publicar los materiales y clases públicos en los índices de Algolia. Requiere iniciar sesión.")]
    public async Task<IActionResult> Reindexar(CancellationToken cancellationToken)
    {
        try
        {
            await busquedaService.ReindexarTodoAsync(cancellationToken);
            return Ok(new { mensaje = "Los índices de Algolia se actualizaron correctamente." });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = exception.Message });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "No se pudieron reindexar los materiales y las clases en Algolia.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Algolia no pudo completar la reindexación." });
        }
    }

    private async Task<IReadOnlyList<ResultadoBusqueda>> BuscarMaterialesPorIdsAsync(
        IReadOnlyList<string> ids,
        bool soloGratuitos,
        CancellationToken cancellationToken)
    {
        var idsNumericos = ConvertirIds(ids);
        var materiales = await db.Materiales.AsNoTracking()
            .Include(item => item.Categoria)
            .Where(item => idsNumericos.Contains(item.Id) && (!soloGratuitos || item.EsSolidario))
            .ToListAsync(cancellationToken);
        var porId = materiales.ToDictionary(item => item.Id);

        return idsNumericos
            .Where(porId.ContainsKey)
            .Select(id => ConvertirResultado(porId[id]))
            .ToArray();
    }

    private async Task<IReadOnlyList<ResultadoBusqueda>> BuscarClasesPorIdsAsync(
        IReadOnlyList<string> ids,
        bool soloGratuitos,
        CancellationToken cancellationToken)
    {
        var idsNumericos = ConvertirIds(ids);
        var clases = await db.Clases.AsNoTracking()
            .Include(item => item.Tutor)
            .Where(item => idsNumericos.Contains(item.Id) && (!soloGratuitos || item.EsSolidaria))
            .ToListAsync(cancellationToken);
        var porId = clases.ToDictionary(item => item.Id);

        return idsNumericos
            .Where(porId.ContainsKey)
            .Select(id => ConvertirResultado(porId[id]))
            .ToArray();
    }

    private async Task<IReadOnlyList<ResultadoBusqueda>> BuscarMaterialesEnPostgresAsync(
        string? texto,
        bool soloGratuitos,
        CancellationToken cancellationToken)
    {
        var consulta = db.Materiales.AsNoTracking()
            .Include(item => item.Categoria)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var patron = $"%{texto}%";
            consulta = consulta.Where(item =>
                EF.Functions.ILike(item.Nombre, patron)
                || EF.Functions.ILike(item.Descripcion, patron)
                || EF.Functions.ILike(item.Carrera, patron)
                || EF.Functions.ILike(item.Categoria.Nombre, patron));
        }

        if (soloGratuitos)
        {
            consulta = consulta.Where(item => item.EsSolidario);
        }

        var materiales = await consulta.OrderByDescending(item => item.FechaPublicacion)
            .Take(100)
            .ToListAsync(cancellationToken);
        return materiales.Select(ConvertirResultado).ToArray();
    }

    private async Task<IReadOnlyList<ResultadoBusqueda>> BuscarClasesEnPostgresAsync(
        string? texto,
        bool soloGratuitos,
        CancellationToken cancellationToken)
    {
        var consulta = db.Clases.AsNoTracking()
            .Include(item => item.Tutor)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var patron = $"%{texto}%";
            consulta = consulta.Where(item =>
                EF.Functions.ILike(item.Curso, patron)
                || EF.Functions.ILike(item.Descripcion, patron)
                || EF.Functions.ILike(item.Tutor.Nombre, patron)
                || EF.Functions.ILike(item.Tutor.Carrera, patron));
        }

        if (soloGratuitos)
        {
            consulta = consulta.Where(item => item.EsSolidaria);
        }

        var clases = await consulta.OrderByDescending(item => item.FechaPublicacion)
            .Take(100)
            .ToListAsync(cancellationToken);
        return clases.Select(ConvertirResultado).ToArray();
    }

    private static int[] ConvertirIds(IEnumerable<string> ids) =>
        ids.Select(id => int.TryParse(id, out var numero) ? numero : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

    private static ResultadoBusqueda ConvertirResultado(Material material) =>
        new(
            material.Id,
            material.Nombre,
            material.Descripcion,
            material.Carrera,
            material.Categoria.Nombre,
            material.PrecioDia,
            material.EsSolidario,
            material.Disponible,
            material.ImagenUrl,
            null,
            null);

    private static ResultadoBusqueda ConvertirResultado(Clase clase) =>
        new(
            clase.Id,
            clase.Curso,
            clase.Descripcion,
            clase.Tutor.Carrera,
            null,
            clase.PrecioHora,
            clase.EsSolidaria,
            true,
            null,
            clase.Tutor.Nombre,
            clase.Modalidad.ToString());

    private sealed record ResultadoBusqueda(
        int Id,
        string Titulo,
        string Descripcion,
        string Carrera,
        string? Categoria,
        decimal Precio,
        bool EsSolidario,
        bool Disponible,
        string? ImagenUrl,
        string? Tutor,
        string? Modalidad);
}
