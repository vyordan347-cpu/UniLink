using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

    public bool AceptaCondiciones { get; set; }

    [ValidateNever]
    public Material? Material { get; set; }
}
