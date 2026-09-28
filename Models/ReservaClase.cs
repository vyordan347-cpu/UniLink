using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class ReservaClase
{
    public int Id { get; set; }

    public int ClaseId { get; set; }
    public Clase Clase { get; set; } = null!;

    public int AlumnoId { get; set; }
    public Usuario Alumno { get; set; } = null!;

    public DateTime FechaHora { get; set; } = DateTime.UtcNow;

    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    [StringLength(1000, ErrorMessage = "El tema no puede superar los 1000 caracteres.")]
    public string? TemaAReforzar { get; set; }
}