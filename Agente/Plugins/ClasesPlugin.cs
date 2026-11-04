using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using UniLink.Data;

namespace UniLink.Agente.Plugins;

[Description("Busca clases ofrecidas por estudiantes en UniLink.")]
public sealed class ClasesPlugin(UniLinkDbContext db, int usuarioId)
{
    [KernelFunction("BuscarClases")]
    [Description("Busca hasta cinco clases por curso y devuelve enlaces relativos a sus detalles.")]
    public async Task<string> BuscarClases(
        [Description("Nombre o palabras del curso que se busca, por ejemplo Anatomía.")] string? curso = null,
        [Description("Indica si solo se deben buscar clases gratuitas o solidarias.")] bool soloGratuitas = false,
        [Description("Token interno para cancelar la consulta.")] CancellationToken cancellationToken = default)
    {
        var consulta = db.Clases.AsNoTracking()
            .Where(clase => clase.TutorId != usuarioId);
        curso = curso?.Trim();
        if (!string.IsNullOrWhiteSpace(curso))
        {
            var cursoNormalizado = curso.ToLowerInvariant();
            consulta = consulta.Where(clase => clase.Curso.ToLower().Contains(cursoNormalizado));
        }

        if (soloGratuitas)
        {
            consulta = consulta.Where(clase => clase.EsSolidaria);
        }

        var clases = await consulta
            .OrderByDescending(clase => clase.FechaPublicacion)
            .Take(5)
            .Select(clase => new
            {
                clase.Id,
                clase.Curso,
                clase.PrecioHora,
                clase.EsSolidaria,
                Tutor = clase.Tutor.Nombre,
                Carrera = clase.Tutor.Carrera
            })
            .ToListAsync(cancellationToken);

        return clases.Count == 0
            ? "No encontré clases con esos filtros."
            : string.Join(Environment.NewLine, clases.Select(clase =>
                $"- {clase.Curso} · {clase.Tutor} ({clase.Carrera}) · " +
                (clase.EsSolidaria ? "gratuita" : $"S/ {clase.PrecioHora:0.00} por hora") +
                $" · [/Clases/Detalle/{clase.Id}](/Clases/Detalle/{clase.Id})"));
    }
}
