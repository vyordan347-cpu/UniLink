using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Models;

namespace UniLink.Controllers;

public class CuentaController(
    UserManager<Usuario> userManager,
    SignInManager<Usuario> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Registro()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        if (await userManager.Users.AnyAsync(usuario => usuario.CodigoAlumno == modelo.CodigoAlumno))
        {
            ModelState.AddModelError(nameof(modelo.CodigoAlumno), "El código de alumno ya está registrado.");
            return View(modelo);
        }

        var usuario = new Usuario
        {
            UserName = modelo.Correo,
            Email = modelo.Correo,
            Nombre = modelo.Nombre,
            CodigoAlumno = modelo.CodigoAlumno,
            Carrera = modelo.Carrera,
            Ciclo = modelo.Ciclo,
            FechaRegistro = DateTime.UtcNow
        };

        var resultado = await userManager.CreateAsync(usuario, modelo.Contraseña);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        TempData["Mensaje"] = "Cuenta creada correctamente";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await signInManager.PasswordSignInAsync(
            modelo.Correo,
            modelo.Contraseña,
            modelo.Recordarme,
            lockoutOnFailure: false);

        if (!resultado.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(modelo);
        }

        var usuario = await userManager.FindByEmailAsync(modelo.Correo);
        if (usuario is null)
        {
            await signInManager.SignOutAsync();
            throw new InvalidOperationException("No se encontró el usuario autenticado.");
        }

        HttpContext.Session.SetString("NombreUsuario", usuario.Nombre);
        TempData["Mensaje"] = "¡Bienvenido a UniLink!";
        return RedirectToAction("Index", "Panel");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        HttpContext.Session.Clear();
        TempData["Mensaje"] = "Sesión cerrada";
        return RedirectToAction("Index", "Home");
    }
}
