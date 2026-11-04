namespace UniLink.Helpers;

public static class CalculadoraAlquiler
{
    public static (int Dias, decimal CostoTotal) Calcular(
        DateOnly fechaInicio,
        DateOnly fechaFin,
        decimal precioDia)
    {
        if (fechaFin < fechaInicio)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fechaFin),
                "La fecha de fin debe ser igual o posterior a la fecha de inicio.");
        }

        var dias = fechaFin.DayNumber - fechaInicio.DayNumber + 1;
        return (dias, dias * precioDia);
    }
}
