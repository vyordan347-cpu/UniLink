using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using UniLink.Data;
using UniLink.Helpers;

namespace UniLink.Agente.Plugins;

[Description("Busca clases ofrecidas por estudiantes en UniLink.")]
public sealed class ClasesPlugin(
    UniLinkDbContext db,
    int usuarioId,
    ILogger<ClasesPlugin> logger)
{
    [KernelFunction("BuscarClases")]
    [Description("Busca hasta cinco clases por curso y devuelve enlaces relativos a sus detalles.")]
    public async Task<string> BuscarClases(
        [Description("Nombre o palabras del curso que se busca, por ejemplo Anatomía.")] string? curso = null,
        [Description("Indica si solo se deben buscar clases gratuitas o solidarias.")] bool soloGratuitas = false,
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        curso = curso?.Trim();
        var clases = await BuscarAsync(curso, soloGratuitas, cancellationToken);
        if (clases.Count == 0 && !string.IsNullOrWhiteSpace(curso) && soloGratuitas)
        {
            clases = await BuscarAsync(curso, false, cancellationToken);
        }

        logger.LogInformation(
            "BuscarClases recibió del LLM curso={Curso}, soloGratuitas={SoloGratuitas}; resultados={CantidadResultados}.",
            curso,
            soloGratuitas,
            clases.Count);

        return clases.Count == 0
            ? "No encontré clases con esos filtros."
            : string.Join(Environment.NewLine, clases.Select(clase =>
                $"- {clase.Curso} · {clase.Tutor} ({clase.Carrera}) · " +
                (clase.EsSolidaria ? "gratuita" : $"S/ {clase.PrecioHora:0.00} por hora") +
                $" · [/Clases/Detalle/{clase.Id}](/Clases/Detalle/{clase.Id})"));
    }

    private async Task<List<ClaseResultado>> BuscarAsync(
        string? curso,
        bool soloGratuitas,
        CancellationToken cancellationToken)
    {
        var consulta = db.Clases.AsNoTracking()
            .Where(clase => clase.TutorId != usuarioId);

        if (!string.IsNullOrWhiteSpace(curso))
        {
            var patron = $"%{TextoBusqueda.NormalizarSinTildes(curso)}%";
            consulta = consulta.Where(clase =>
                EF.Functions.ILike(UniLinkDbContext.Unaccent(clase.Curso), patron)
                || EF.Functions.ILike(UniLinkDbContext.Unaccent(clase.Descripcion), patron)
                || EF.Functions.ILike(UniLinkDbContext.Unaccent(clase.Tutor.Nombre), patron)
                || EF.Functions.ILike(UniLinkDbContext.Unaccent(clase.Tutor.Carrera), patron));
        }

        if (soloGratuitas)
        {
            consulta = consulta.Where(clase => clase.EsSolidaria);
        }

        return await consulta
            .OrderByDescending(clase => clase.FechaPublicacion)
            .Take(5)
            .Select(clase => new ClaseResultado(
                clase.Id,
                clase.Curso,
                clase.PrecioHora,
                clase.EsSolidaria,
                clase.Tutor.Nombre,
                clase.Tutor.Carrera))
            .ToListAsync(cancellationToken);
    }

    private sealed record ClaseResultado(
        int Id,
        string Curso,
        decimal PrecioHora,
        bool EsSolidaria,
        string Tutor,
        string Carrera);
}
