using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniLink.Controllers;

[Authorize]
public class PanelController : Controller
{
    public IActionResult Index()
    {
        ViewData["NombreUsuario"] = HttpContext.Session.GetString("NombreUsuario") ?? User.Identity?.Name;
        ViewData["AlquileresActivos"] = 0;
        ViewData["ClasesProximas"] = 0;
        ViewData["SolicitudesPendientes"] = 0;
        return View();
    }
}
