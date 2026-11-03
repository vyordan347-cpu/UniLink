using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace UniLink.Models;

public class ListaAlquileresViewModel
{
    [ValidateNever]
    public IReadOnlyList<Alquiler> Alquileres { get; set; } = [];

    public bool MostrarDiasParaDevolver { get; set; }
}
