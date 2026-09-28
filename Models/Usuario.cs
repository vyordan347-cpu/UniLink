using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código de alumno es obligatorio.")]
    [StringLength(20, ErrorMessage = "El código de alumno no puede superar los 20 caracteres.")]
    public string CodigoAlumno { get; set; } = string.Empty;

    [Required(ErrorMessage = "La carrera es obligatoria.")]
    [StringLength(100, ErrorMessage = "La carrera no puede superar los 100 caracteres.")]
    public string Carrera { get; set; } = string.Empty;

    [Range(1, 14, ErrorMessage = "El ciclo debe estar entre 1 y 14.")]
    public int Ciclo { get; set; }

    [Required(ErrorMessage = "La contraseña cifrada es obligatoria.")]
    [StringLength(255, ErrorMessage = "La contraseña cifrada no puede superar los 255 caracteres.")]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<Material> MaterialesPublicados { get; set; } = new List<Material>();
    public ICollection<Alquiler> SolicitudesAlquiler { get; set; } = new List<Alquiler>();
    public ICollection<Clase> ClasesPublicadas { get; set; } = new List<Clase>();
    public ICollection<ReservaClase> ReservasClase { get; set; } = new List<ReservaClase>();
    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
}