using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace UniLink.Models;

public class ReservaClaseViewModel
{
    public int ClaseId { get; set; }

    [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
    [DataType(DataType.DateTime)]
    public DateTime FechaHora { get; set; }

    [Range(1, 3, ErrorMessage = "La duración debe ser de 1 a 3 horas.")]
    public int DuracionHoras { get; set; } = 1;

    [StringLength(1000, ErrorMessage = "El tema no puede superar los 1000 caracteres.")]
    public string? TemaAReforzar { get; set; }

    public bool AceptaCondiciones { get; set; }

    public int DesfaseHorarioMinutos { get; set; }

    [ValidateNever]
    public Clase? Clase { get; set; }
}
