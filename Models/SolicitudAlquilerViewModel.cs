using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class SolicitudAlquilerViewModel
{
    public int MaterialId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    public DateOnly FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de devolución es obligatoria.")]
    [DataType(DataType.Date)]
    public DateOnly FechaFin { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Debes aceptar las condiciones del préstamo.")]
    public bool AceptaCondiciones { get; set; }

    public Material Material { get; set; } = null!;
}
