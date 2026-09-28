using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniLink.Models;

public class Alquiler
{
    public int Id { get; set; }

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public int SolicitanteId { get; set; }
    public Usuario Solicitante { get; set; } = null!;

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateOnly FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    public DateOnly FechaFin { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    [Range(0, 99999999.99, ErrorMessage = "El costo total debe ser de 0 a 99 999 999,99 soles.")]
    public decimal CostoTotal { get; set; }

    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
}