namespace UniLink.Models;

public class SolidarioViewModel
{
    public IReadOnlyList<Material> Materiales { get; set; } = [];
    public IReadOnlyList<Clase> Clases { get; set; } = [];
}
