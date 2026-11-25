using HocicosAJ.Data;
using HocicosAJ.Models.Database;
using HocicosAJ.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Policy = "Administradores")]
public class AdminCategoriasController : Controller
{
    private readonly HocicosAjContext _context;
    private readonly ILogger<AdminCategoriasController> _logger;

    public AdminCategoriasController(
        HocicosAjContext context,
        ILogger<AdminCategoriasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var categorias = await _context.Categoria
            .AsNoTracking()
            .Include(categoria => categoria.Productos)
            .OrderBy(categoria => categoria.NombreCategoria)
            .ToListAsync();

        return View(categorias);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View(new CategoriaFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CategoriaFormViewModel modelo)
    {
        var nombre = modelo.NombreCategoria?.Trim() ?? string.Empty;
        if (nombre.Length > 0 && await NombreCategoriaExisteAsync(nombre))
        {
            ModelState.AddModelError(
                nameof(modelo.NombreCategoria),
                "Ya existe una categoría con ese nombre.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var categoria = new Categorium
        {
            NombreCategoria = nombre,
            Descripcion = LimpiarOpcional(modelo.Descripcion),
            Activo = true
        };

        try
        {
            await _context.Categoria.AddAsync(categoria);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "No se pudo registrar la categoría.");
            ModelState.AddModelError(
                string.Empty,
                "No fue posible registrar la categoría. Verifique los datos e inténtelo nuevamente.");
            return View(modelo);
        }

        TempData["Success"] = "La categoría se registró correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var categoria = await _context.Categoria
            .AsNoTracking()
            .FirstOrDefaultAsync(categoria => categoria.Idcategoria == id);

        if (categoria is null)
        {
            return NotFound();
        }

        return View(new CategoriaFormViewModel
        {
            NombreCategoria = categoria.NombreCategoria,
            Descripcion = categoria.Descripcion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, CategoriaFormViewModel modelo)
    {
        var categoria = await _context.Categoria
            .FirstOrDefaultAsync(categoria => categoria.Idcategoria == id);

        if (categoria is null)
        {
            return NotFound();
        }

        var nombre = modelo.NombreCategoria?.Trim() ?? string.Empty;
        if (nombre.Length > 0 && await NombreCategoriaExisteAsync(nombre, id))
        {
            ModelState.AddModelError(
                nameof(modelo.NombreCategoria),
                "Ya existe una categoría con ese nombre.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        categoria.NombreCategoria = nombre;
        categoria.Descripcion = LimpiarOpcional(modelo.Descripcion);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "No se pudo actualizar la categoría {CategoriaId}.", id);
            ModelState.AddModelError(
                string.Empty,
                "No fue posible actualizar la categoría. Verifique los datos e inténtelo nuevamente.");
            return View(modelo);
        }

        TempData["Success"] = "La categoría se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var categoria = await _context.Categoria
            .FirstOrDefaultAsync(categoria => categoria.Idcategoria == id);

        if (categoria is null)
        {
            return NotFound();
        }

        if (!categoria.Activo)
        {
            TempData["Error"] = "La categoría ya está desactivada.";
            return RedirectToAction(nameof(Index));
        }

        if (await _context.Productos.AnyAsync(producto =>
                producto.CategoriaIdcategoria == id))
        {
            TempData["Error"] =
                "No se puede desactivar la categoría mientras tenga productos asociados. Reasigne o retire esos productos primero.";
            return RedirectToAction(nameof(Index));
        }

        categoria.Activo = false;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "No se pudo desactivar la categoría {CategoriaId}.", id);
            TempData["Error"] = "No fue posible desactivar la categoría.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "La categoría se desactivó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var categoria = await _context.Categoria
            .FirstOrDefaultAsync(categoria => categoria.Idcategoria == id);

        if (categoria is null)
        {
            return NotFound();
        }

        if (categoria.Activo)
        {
            TempData["Error"] = "La categoría ya está activa.";
            return RedirectToAction(nameof(Index));
        }

        categoria.Activo = true;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "No se pudo reactivar la categoría {CategoriaId}.", id);
            TempData["Error"] = "No fue posible reactivar la categoría.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "La categoría se reactivó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> NombreCategoriaExisteAsync(string nombre, int? excluirId = null)
    {
        return await _context.Categoria.AnyAsync(categoria =>
            categoria.NombreCategoria.ToLower() == nombre.ToLower()
            && (!excluirId.HasValue || categoria.Idcategoria != excluirId.Value));
    }

    private static string? LimpiarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
