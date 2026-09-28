using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre de la categoría no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Material> Materiales { get; set; } = new List<Material>();
}