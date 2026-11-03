using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class Resena
{
    public int Id { get; set; }

    public int AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;

    public int UsuarioReseñadoId { get; set; }
    public Usuario UsuarioReseñado { get; set; } = null!;

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public int? ClaseId { get; set; }
    public Clase? Clase { get; set; }

    public int? AlquilerId { get; set; }
    public Alquiler? Alquiler { get; set; }

    public int? ReservaClaseId { get; set; }
    public ReservaClase? ReservaClase { get; set; }

    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5 estrellas.")]
    public int Calificacion { get; set; }

    [StringLength(500, ErrorMessage = "El comentario no puede superar los 500 caracteres.")]
    public string Comentario { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Sentimiento { get; set; } = string.Empty;

    public float Probabilidad { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
