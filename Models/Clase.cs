using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniLink.Models;

public class Clase
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del curso es obligatorio.")]
    [StringLength(150, ErrorMessage = "El curso no puede superar los 150 caracteres.")]
    public string Curso { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    public ModalidadClase Modalidad { get; set; }

    [Required(ErrorMessage = "El enlace de la reunión es obligatorio.")]
    [StringLength(500, ErrorMessage = "El enlace no puede superar los 500 caracteres.")]
    public string EnlaceReunion { get; set; } = string.Empty;

    [Column(TypeName = "numeric(10,2)")]
    [Range(0, 99999999.99, ErrorMessage = "El precio por hora debe ser de 0 a 99 999 999,99 soles.")]
    public decimal PrecioHora { get; set; }

    public bool EsSolidaria { get; set; }

    public int TutorId { get; set; }
    public Usuario Tutor { get; set; } = null!;

    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    public ICollection<ReservaClase> Reservas { get; set; } = new List<ReservaClase>();
}