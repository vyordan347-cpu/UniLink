namespace UniLink.Models;

public class MisClasesViewModel
{
    public string Pestaña { get; set; } = "alumno";
    public IReadOnlyList<ReservaClase> ReservasComoAlumno { get; set; } = [];
    public IReadOnlyList<ReservaClase> ReservasComoTutor { get; set; } = [];
    public IReadOnlyList<Clase> ClasesComoTutor { get; set; } = [];
}
