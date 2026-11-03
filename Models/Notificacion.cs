using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class Notificacion
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    [StringLength(500, ErrorMessage = "El mensaje no puede superar los 500 caracteres.")]
    public string Mensaje { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La URL no puede superar los 500 caracteres.")]
    public string? Url { get; set; }

    public bool Leida { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}