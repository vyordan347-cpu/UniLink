namespace UniLink.Models;

public sealed class RecomendacionViewModel
{
    public string Tipo { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string UnidadPrecio { get; set; } = string.Empty;
    public bool EsSolidario { get; set; }
    public string? ImagenUrl { get; set; }
    public string UrlDetalle { get; set; } = string.Empty;
}
