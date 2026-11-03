using System.ComponentModel.DataAnnotations;
namespace UniLink.Models;

public class ClaseViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El curso es obligatorio.")]
    [StringLength(150, ErrorMessage = "El curso no puede superar los 150 caracteres.")]
    public string Curso { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    public ModalidadClase Modalidad { get; set; } = ModalidadClase.VideollamadaUniLink;

    public decimal PrecioHora { get; set; }

    public bool EsSolidaria { get; set; }
}
