using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniLink.Models;

public class Material
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del material es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La carrera es obligatoria.")]
    [StringLength(100, ErrorMessage = "La carrera no puede superar los 100 caracteres.")]
    public string Carrera { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría válida.")]
    public int CategoriaId { get; set; }

    public CondicionMaterial Condicion { get; set; }

    public bool Esterilizado { get; set; }

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(150, ErrorMessage = "La ubicación no puede superar los 150 caracteres.")]
    public string Ubicacion { get; set; } = string.Empty;

    [Column(TypeName = "numeric(10,2)")]
    [Range(0, 99999999.99, ErrorMessage = "El precio por día debe ser de 0 a 99 999 999,99 soles.")]
    public decimal PrecioDia { get; set; }

    public bool EsSolidario { get; set; }

    [StringLength(500, ErrorMessage = "La URL de imagen no puede superar los 500 caracteres.")]
    public string? ImagenUrl { get; set; }

    public bool Disponible { get; set; } = true;

    public int PropietarioId { get; set; }
    public Usuario Propietario { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;

    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    public ICollection<Alquiler> Alquileres { get; set; } = new List<Alquiler>();
}