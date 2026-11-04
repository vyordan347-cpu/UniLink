namespace UniLink.Models;

public class PerfilViewModel
{
    public Usuario Usuario { get; set; } = null!;
    public ResenasResumenViewModel ResumenResenas { get; set; } = new();
    public IReadOnlyList<Material> Materiales { get; set; } = [];
    public IReadOnlyList<Clase> Clases { get; set; } = [];
    public bool EsEstudianteSolidario { get; set; }
}
