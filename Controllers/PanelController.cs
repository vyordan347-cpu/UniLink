using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniLink.Services;

namespace UniLink.Controllers;

[Authorize]
public class PanelController : Controller
{
    private readonly ICategoriaService _categoriaService;

    public PanelController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["NombreUsuario"] = HttpContext.Session.GetString("NombreUsuario") ?? User.Identity?.Name;
        ViewData["AlquileresActivos"] = 0;
        ViewData["ClasesProximas"] = 0;
        ViewData["SolicitudesPendientes"] = 0;

        var categorias = await _categoriaService.ObtenerCategoriasAsync(cancellationToken);
        return View(categorias);
    }
}
