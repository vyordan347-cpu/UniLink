using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public class MaterialViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del material es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre del material")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría válida.")]
    [Display(Name = "Categoría")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "La carrera es obligatoria.")]
    [StringLength(100, ErrorMessage = "La carrera no puede superar los 100 caracteres.")]
    [Display(Name = "Carrera")]
    public string Carrera { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [EnumDataType(typeof(CondicionMaterial), ErrorMessage = "Selecciona una condición válida.")]
    [Display(Name = "Condición")]
    public CondicionMaterial Condicion { get; set; }

    [Display(Name = "Está esterilizado")]
    public bool Esterilizado { get; set; }

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(150, ErrorMessage = "La ubicación no puede superar los 150 caracteres.")]
    [Display(Name = "Ubicación")]
    public string Ubicacion { get; set; } = string.Empty;

    [Url(ErrorMessage = "Ingresa una URL de imagen válida.")]
    [StringLength(500, ErrorMessage = "La URL de imagen no puede superar los 500 caracteres.")]
    [Display(Name = "URL de imagen (opcional)")]
    public string? ImagenUrl { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "El precio por día debe ser de 0 a 99 999 999,99 soles.")]
    [Display(Name = "Precio por día (S/)")]
    public decimal PrecioDia { get; set; }

    [Display(Name = "Quiero ofrecerlo como préstamo solidario")]
    public bool EsSolidario { get; set; }
}
