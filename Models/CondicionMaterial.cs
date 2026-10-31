using System.ComponentModel.DataAnnotations;

namespace UniLink.Models;

public enum CondicionMaterial
{
    [Display(Name = "Excelente")]
    Excelente,
    [Display(Name = "Muy bueno")]
    MuyBueno,
    [Display(Name = "Bueno")]
    Bueno,
    [Display(Name = "Regular")]
    Regular
}