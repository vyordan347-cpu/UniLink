using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;

namespace UniLink.Controllers;

public class SolidarioController(UniLinkDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var modelo = new SolidarioViewModel
        {
            Materiales = await db.Materiales.AsNoTracking()
                .Include(material => material.Propietario)
                .Include(material => material.Categoria)
                .Where(material => material.EsSolidario)
                .OrderByDescending(material => material.FechaPublicacion)
                .ToListAsync(cancellationToken),
            Clases = await db.Clases.AsNoTracking()
                .Include(clase => clase.Tutor)
                .Where(clase => clase.EsSolidaria)
                .OrderByDescending(clase => clase.FechaPublicacion)
                .ToListAsync(cancellationToken)
        };

        ViewData["Title"] = "UniLink Solidario";
        return View(modelo);
    }
}
