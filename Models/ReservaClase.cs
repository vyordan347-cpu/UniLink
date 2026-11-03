using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace UniLink.Models;

public class ReservaClase
{
    public int Id { get; set; }

    public int ClaseId { get; set; }
    [ValidateNever]
    public Clase Clase { get; set; } = null!;

    public int AlumnoId { get; set; }
    [ValidateNever]
    public Usuario Alumno { get; set; } = null!;

    public DateTime FechaHora { get; set; } = DateTime.UtcNow;

    [Range(1, 3, ErrorMessage = "La duración debe ser de 1 a 3 horas.")]
    public int DuracionHoras { get; set; } = 1;

    [Column(TypeName = "numeric(10,2)")]
    [Range(0, 99999999.99, ErrorMessage = "El costo total supera el máximo permitido.")]
    public decimal CostoTotal { get; set; }

    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    [StringLength(1000, ErrorMessage = "El tema no puede superar los 1000 caracteres.")]
    public string? TemaAReforzar { get; set; }

    [StringLength(500, ErrorMessage = "El enlace no puede superar los 500 caracteres.")]
    public string? EnlaceReunion { get; set; }
}