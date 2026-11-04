namespace UniLink.Models;

public class ResenasResumenViewModel
{
    public IReadOnlyList<Resena> Resenas { get; set; } = [];
    public double PromedioCalificacion { get; set; }
}
