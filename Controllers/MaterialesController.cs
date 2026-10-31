using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniLink.Data;
using UniLink.Models;
using UniLink.Services;

namespace UniLink.Controllers;

public class MaterialesController(
    UniLinkDbContext db,
    ICategoriaService categoriaService,
    ILogger<MaterialesController> logger) : Controller
{
    private const string CookieFiltro = "ul_filtro_materiales";
    private const string ImagenPredeterminada = "/img/material-default.svg";
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int? categoriaId,
        [FromQuery] string? carrera,
        [FromQuery] decimal? precioMaximo,
        [FromQuery] bool soloGratuitos,
        [FromQuery] bool limpiarFiltros,
        CancellationToken cancellationToken)
    {
        var filtro = new FiltroMaterialesCookie(categoriaId, carrera, precioMaximo, soloGratuitos);
        if (limpiarFiltros)
        {
            filtro = new FiltroMaterialesCookie(null, null, null, false);
        }
        else if (Request.Query.Count == 0 && Request.Cookies.TryGetValue(CookieFiltro, out var filtroGuardado))
        {
            try
            {
                filtro = JsonSerializer.Deserialize<FiltroMaterialesCookie>(filtroGuardado, OpcionesJson)
                    ?? new FiltroMaterialesCookie(null, null, null, false);
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Se ignoró una cookie de filtros de materiales no válida.");
                Response.Cookies.Delete(CookieFiltro);
                filtro = new FiltroMaterialesCookie(null, null, null, false);
            }
        }

        filtro = NormalizarFiltro(filtro);
        if (Request.Query.Count > 0)
        {
            Response.Cookies.Append(
                CookieFiltro,
                JsonSerializer.Serialize(filtro, OpcionesJson),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps
                });
        }

        var consulta = db.Materiales
            .AsNoTracking()
            .Include(material => material.Propietario)
            .Include(material => material.Categoria)
            .AsQueryable();

        if (filtro.CategoriaId is > 0)
        {
            consulta = consulta.Where(material => material.CategoriaId == filtro.CategoriaId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Carrera))
        {
            consulta = consulta.Where(material => material.Carrera == filtro.Carrera);
        }

        if (filtro.PrecioMaximo is >= 0)
        {
            consulta = consulta.Where(material => material.PrecioDia <= filtro.PrecioMaximo);
        }

        if (filtro.SoloGratuitos)
        {
            consulta = consulta.Where(material => material.EsSolidario);
        }

        var modelo = new MaterialesExplorarViewModel
        {
            Materiales = await consulta.OrderByDescending(material => material.FechaPublicacion)
                .ToListAsync(cancellationToken),
            Categorias = await categoriaService.ObtenerCategoriasAsync(cancellationToken),
            Carreras = await db.Materiales.AsNoTracking()
                .Where(material => material.Carrera != string.Empty)
                .Select(material => material.Carrera)
                .Distinct()
                .OrderBy(nombreCarrera => nombreCarrera)
                .ToListAsync(cancellationToken),
            CategoriaId = filtro.CategoriaId,
            Carrera = filtro.Carrera,
            PrecioMaximo = filtro.PrecioMaximo,
            SoloGratuitos = filtro.SoloGratuitos
        };

        ViewData["Title"] = "Explorar materiales";
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        var material = await db.Materiales
            .AsNoTracking()
            .Include(item => item.Propietario)
            .Include(item => item.Categoria)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (material is null)
        {
            return NotFound();
        }

        ViewData["EsPropietario"] = EsPropietario(material);
        ViewData["Title"] = material.Nombre;
        return View(material);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Publicar(CancellationToken cancellationToken)
    {
        await CargarCategoriasAsync(cancellationToken);
        ViewData["Title"] = "Publicar material";
        return View(new MaterialViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publicar(MaterialViewModel modelo, CancellationToken cancellationToken)
    {
        if (modelo.EsSolidario)
        {
            modelo.PrecioDia = 0;
        }
        else if (modelo.PrecioDia == 0)
        {
            ModelState.AddModelError(nameof(modelo.PrecioDia), "Marca la opción de préstamo solidario para ofrecerlo gratis.");
        }

        if (!await db.Categorias.AnyAsync(categoria => categoria.Id == modelo.CategoriaId, cancellationToken))
        {
            ModelState.AddModelError(nameof(modelo.CategoriaId), "Selecciona una categoría válida.");
        }

        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(cancellationToken);
            ViewData["Title"] = "Publicar material";
            return View(modelo);
        }

        var propietarioId = ObtenerUsuarioId();
        if (propietarioId is null)
        {
            return Forbid();
        }

        var material = new Material
        {
            Nombre = modelo.Nombre.Trim(),
            CategoriaId = modelo.CategoriaId,
            Carrera = modelo.Carrera.Trim(),
            Descripcion = modelo.Descripcion.Trim(),
            Condicion = modelo.Condicion,
            Esterilizado = modelo.Esterilizado,
            Ubicacion = modelo.Ubicacion.Trim(),
            ImagenUrl = string.IsNullOrWhiteSpace(modelo.ImagenUrl) ? ImagenPredeterminada : modelo.ImagenUrl.Trim(),
            PrecioDia = modelo.EsSolidario ? 0 : modelo.PrecioDia,
            EsSolidario = modelo.EsSolidario,
            PropietarioId = propietarioId.Value,
            FechaPublicacion = DateTime.UtcNow
        };

        db.Materiales.Add(material);
        await db.SaveChangesAsync(cancellationToken);

        TempData["Mensaje"] = "¡Material publicado!";
        return RedirectToAction(nameof(Detalle), new { id = material.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
    {
        var material = await db.Materiales
            .AsNoTracking()
            .Include(item => item.Categoria)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (material is null)
        {
            return NotFound();
        }

        if (!EsPropietario(material))
        {
            return Forbid();
        }

        await CargarCategoriasAsync(cancellationToken);
        ViewData["Title"] = "Editar material";
        return View(ConvertirViewModel(material));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, MaterialViewModel modelo, CancellationToken cancellationToken)
    {
        modelo.Id = id;
        var material = await db.Materiales
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (material is null)
        {
            return NotFound();
        }

        if (!EsPropietario(material))
        {
            return Forbid();
        }

        if (modelo.EsSolidario)
        {
            modelo.PrecioDia = 0;
        }
        else if (modelo.PrecioDia == 0)
        {
            ModelState.AddModelError(nameof(modelo.PrecioDia), "Marca la opción de préstamo solidario para ofrecerlo gratis.");
        }

        if (!await db.Categorias.AnyAsync(categoria => categoria.Id == modelo.CategoriaId, cancellationToken))
        {
            ModelState.AddModelError(nameof(modelo.CategoriaId), "Selecciona una categoría válida.");
        }

        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(cancellationToken);
            ViewData["Title"] = "Editar material";
            return View(modelo);
        }

        material.Nombre = modelo.Nombre.Trim();
        material.CategoriaId = modelo.CategoriaId;
        material.Carrera = modelo.Carrera.Trim();
        material.Descripcion = modelo.Descripcion.Trim();
        material.Condicion = modelo.Condicion;
        material.Esterilizado = modelo.Esterilizado;
        material.Ubicacion = modelo.Ubicacion.Trim();
        material.ImagenUrl = string.IsNullOrWhiteSpace(modelo.ImagenUrl) ? ImagenPredeterminada : modelo.ImagenUrl.Trim();
        material.PrecioDia = modelo.EsSolidario ? 0 : modelo.PrecioDia;
        material.EsSolidario = modelo.EsSolidario;

        await db.SaveChangesAsync(cancellationToken);
        TempData["Mensaje"] = "Material actualizado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = material.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var material = await db.Materiales
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (material is null)
        {
            return NotFound();
        }

        if (!EsPropietario(material))
        {
            return Forbid();
        }

        if (await db.Alquileres.AnyAsync(alquiler => alquiler.MaterialId == id, cancellationToken))
        {
            TempData["Error"] = "No se puede eliminar un material que tiene solicitudes o alquileres registrados.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        db.Materiales.Remove(material);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Mensaje"] = "Material eliminado correctamente.";
        return RedirectToAction(nameof(Mios));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mios(CancellationToken cancellationToken)
    {
        var propietarioId = ObtenerUsuarioId();
        if (propietarioId is null)
        {
            return Forbid();
        }

        var materiales = await db.Materiales
            .AsNoTracking()
            .Include(material => material.Propietario)
            .Include(material => material.Categoria)
            .Where(material => material.PropietarioId == propietarioId.Value)
            .OrderByDescending(material => material.FechaPublicacion)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Mis materiales";
        ViewData["MostrarAccionesPropietario"] = true;
        return View(materiales);
    }

    private async Task CargarCategoriasAsync(CancellationToken cancellationToken)
    {
        var categorias = await categoriaService.ObtenerCategoriasAsync(cancellationToken);
        ViewData["Categorias"] = categorias
            .Select(categoria => new SelectListItem(categoria.Nombre, categoria.Id.ToString()))
            .ToList();
    }

    private int? ObtenerUsuarioId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var usuarioId) ? usuarioId : null;
    }

    private bool EsPropietario(Material material) =>
        ObtenerUsuarioId() is int usuarioId && material.PropietarioId == usuarioId;

    private static MaterialViewModel ConvertirViewModel(Material material) =>
        new()
        {
            Id = material.Id,
            Nombre = material.Nombre,
            CategoriaId = material.CategoriaId,
            Carrera = material.Carrera,
            Descripcion = material.Descripcion,
            Condicion = material.Condicion,
            Esterilizado = material.Esterilizado,
            Ubicacion = material.Ubicacion,
            ImagenUrl = material.ImagenUrl,
            PrecioDia = material.PrecioDia,
            EsSolidario = material.EsSolidario
        };

    private static FiltroMaterialesCookie NormalizarFiltro(FiltroMaterialesCookie filtro)
    {
        var carrera = filtro.Carrera?.Trim();
        if (string.IsNullOrEmpty(carrera))
        {
            carrera = null;
        }
        else if (carrera.Length > 100)
        {
            carrera = carrera[..100];
        }

        return new FiltroMaterialesCookie(
            filtro.CategoriaId is > 0 ? filtro.CategoriaId : null,
            carrera,
            filtro.PrecioMaximo is >= 0 ? filtro.PrecioMaximo : null,
            filtro.SoloGratuitos);
    }

    private sealed class FiltroMaterialesCookie
    {
        public FiltroMaterialesCookie()
        {
        }

        public FiltroMaterialesCookie(
            int? categoriaId,
            string? carrera,
            decimal? precioMaximo,
            bool soloGratuitos)
        {
            CategoriaId = categoriaId;
            Carrera = carrera;
            PrecioMaximo = precioMaximo;
            SoloGratuitos = soloGratuitos;
        }

        public int? CategoriaId { get; set; }
        public string? Carrera { get; set; }
        public decimal? PrecioMaximo { get; set; }
        public bool SoloGratuitos { get; set; }
    }
}
