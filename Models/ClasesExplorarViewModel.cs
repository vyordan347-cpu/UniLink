namespace UniLink.Models;

public class ClasesExplorarViewModel
{
    public IReadOnlyList<Clase> Clases { get; set; } = [];
    public IReadOnlyList<string> Carreras { get; set; } = [];
    public string? Curso { get; set; }
    public string? Carrera { get; set; }
    public bool SoloGratuitas { get; set; }
}
