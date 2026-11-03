namespace UniLink.Models;

public class PerfilViewModel
{
    public Usuario Usuario { get; set; } = null!;
    public ResenasResumenViewModel ResumenResenas { get; set; } = new();
}
