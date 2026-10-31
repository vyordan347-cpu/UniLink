namespace UniLink.Models;

public class ListaAlquileresViewModel
{
    public IReadOnlyList<Alquiler> Alquileres { get; set; } = [];
    public bool MostrarDiasParaDevolver { get; set; }
}
