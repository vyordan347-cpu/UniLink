using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniLink.Models;

namespace UniLink.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var codigo = statusCode ?? StatusCodes.Status500InternalServerError;
        Response.StatusCode = codigo;
        ViewData["Title"] = codigo == StatusCodes.Status404NotFound ? "Página no encontrada" : "Ocurrió un error";
        ViewData["MetaDescription"] = codigo == StatusCodes.Status404NotFound
            ? "La página que buscas no está disponible en UniLink."
            : "No pudimos completar tu solicitud. Inténtalo nuevamente más tarde.";

        return codigo == StatusCodes.Status404NotFound
            ? View("Error404", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier })
            : View("Error500", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
