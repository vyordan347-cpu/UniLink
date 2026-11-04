using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class ResenaCrearViewModel
{
    public int OperacionId { get; set; }

    [Range(1, 5, ErrorMessage = "Elige una calificación de 1 a 5 estrellas.")]
    public int Calificacion { get; set; }

    [Required(ErrorMessage = "Escribe un comentario.")]
    [StringLength(500, ErrorMessage = "El comentario no puede superar los 500 caracteres.")]
    public string Comentario { get; set; } = string.Empty;
}
