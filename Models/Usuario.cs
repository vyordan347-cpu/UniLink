using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace UniLink.Models;

public class Usuario : IdentityUser<int>
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código de alumno es obligatorio.")]
    [StringLength(20, ErrorMessage = "El código de alumno no puede superar los 20 caracteres.")]
    public string CodigoAlumno { get; set; } = string.Empty;

    [Required(ErrorMessage = "La carrera es obligatoria.")]
    [StringLength(100, ErrorMessage = "La carrera no puede superar los 100 caracteres.")]
    public string Carrera { get; set; } = string.Empty;

    [Range(1, 14, ErrorMessage = "El ciclo debe estar entre 1 y 14.")]
    public int Ciclo { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<Material> MaterialesPublicados { get; set; } = new List<Material>();
    public ICollection<Alquiler> SolicitudesAlquiler { get; set; } = new List<Alquiler>();
    public ICollection<Clase> ClasesPublicadas { get; set; } = new List<Clase>();
    public ICollection<ReservaClase> ReservasClase { get; set; } = new List<ReservaClase>();
    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
}