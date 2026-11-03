using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace UniLink.Models;

public class MaterialesExplorarViewModel
{
    [ValidateNever]
    public IReadOnlyList<Material> Materiales { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<Categoria> Categorias { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<string> Carreras { get; set; } = [];
    public int? CategoriaId { get; set; }
    public string? Carrera { get; set; }
    public decimal? PrecioMaximo { get; set; }
    public bool SoloGratuitos { get; set; }
}
