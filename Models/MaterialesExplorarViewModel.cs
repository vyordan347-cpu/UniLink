namespace UniLink.Models;

public class MaterialesExplorarViewModel
{
    public IReadOnlyList<Material> Materiales { get; set; } = [];
    public IReadOnlyList<Categoria> Categorias { get; set; } = [];
    public IReadOnlyList<string> Carreras { get; set; } = [];
    public int? CategoriaId { get; set; }
    public string? Carrera { get; set; }
    public decimal? PrecioMaximo { get; set; }
    public bool SoloGratuitos { get; set; }
}
