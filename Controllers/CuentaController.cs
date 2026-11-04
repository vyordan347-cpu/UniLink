using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;

namespace UniLink.Controllers;

public class CuentaController(
    UserManager<Usuario> userManager,
    SignInManager<Usuario> signInManager,
    UniLinkDbContext db) : Controller
{
    [HttpGet("/Perfil/{id:int}")]
    [HttpGet("/Cuenta/Perfil/{id:int}")]
    public async Task<IActionResult> Perfil(int id, CancellationToken cancellationToken)
    {
        var usuario = await db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (usuario is null)
        {
            return NotFound();
        }

        var resenas = await db.Resenas.AsNoTracking()
            .Include(resena => resena.Autor)
            .Include(resena => resena.Material)
            .Include(resena => resena.Clase)
            .Where(resena => resena.UsuarioReseñadoId == id)
            .OrderByDescending(resena => resena.Fecha)
            .ToListAsync(cancellationToken);
        var materiales = await db.Materiales.AsNoTracking()
            .Include(material => material.Categoria)
            .Where(material => material.PropietarioId == id)
            .OrderByDescending(material => material.FechaPublicacion)
            .ToListAsync(cancellationToken);
        var clases = await db.Clases.AsNoTracking()
            .Where(clase => clase.TutorId == id)
            .OrderByDescending(clase => clase.FechaPublicacion)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = $"Perfil de {usuario.Nombre}";
        ViewData["MetaDescription"] = $"Conoce a {usuario.Nombre}, estudiante de {usuario.Carrera}, y descubre sus materiales, clases y reseñas en UniLink.";
        return View(new PerfilViewModel
        {
            Usuario = usuario,
            ResumenResenas = new ResenasResumenViewModel
            {
                Resenas = resenas,
                PromedioCalificacion = resenas.Count == 0 ? 0 : resenas.Average(resena => resena.Calificacion)
            },
            Materiales = materiales,
            Clases = clases,
            EsEstudianteSolidario = materiales.Any(material => material.EsSolidario || material.PrecioDia == 0)
                || clases.Any(clase => clase.EsSolidaria || clase.PrecioHora == 0)
        });
    }

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
