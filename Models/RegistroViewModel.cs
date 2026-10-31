using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class RegistroViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [RegularExpression(@"(?i)^[^@\s]+@usmp\.pe$", ErrorMessage = "Solo se permiten correos institucionales @usmp.pe")]
    [StringLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código de alumno es obligatorio.")]
    [StringLength(20, ErrorMessage = "El código de alumno no puede superar los 20 caracteres.")]
    public string CodigoAlumno { get; set; } = string.Empty;

    [Required(ErrorMessage = "La carrera es obligatoria.")]
    [StringLength(100, ErrorMessage = "La carrera no puede superar los 100 caracteres.")]
    public string Carrera { get; set; } = string.Empty;

    [Range(1, 14, ErrorMessage = "El ciclo debe estar entre 1 y 14.")]
    public int Ciclo { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    public string Contraseña { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Contraseña), ErrorMessage = "Las contraseñas no coinciden.")]
    public string Confirmar { get; set; } = string.Empty;
}
