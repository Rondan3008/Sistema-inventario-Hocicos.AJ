using HocicosAJ.Data;
using HocicosAJ.Models.Database;
using HocicosAJ.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HocicosAJ.Services;

namespace HocicosAJ.Controllers;

[Authorize(Policy = "Administradores")]
public class AdminProductosController : Controller
{
    private readonly HocicosAjContext _context;
    private readonly ILogger<AdminProductosController> _logger;
    private readonly StorefrontAssetStore _assetStore;

    public AdminProductosController(
        HocicosAjContext context,
        ILogger<AdminProductosController> logger,
        StorefrontAssetStore assetStore)
    {
        _context = context;
        _logger = logger;
        _assetStore = assetStore;
    }

    public async Task<IActionResult> Index()
    {
        var productos = await _context.Productos
            .AsNoTracking()
            .Include(producto => producto.CategoriaIdcategoriaNavigation)
            .OrderBy(producto => producto.Nombre)
            .ToListAsync();

        return View(productos);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarCategoriasAsync();
        return View(new ProductoFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProductoFormViewModel modelo)
    {
        ValidarPrecisionPrecio(modelo.Precio);
        if (!await CategoriaSeleccionableAsync(modelo.CategoriaIdcategoria))
        {
            ModelState.AddModelError(
                nameof(modelo.CategoriaIdcategoria),
                "Seleccione una categoría activa.");
        }

        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }

        var producto = new Producto
        {
            Nombre = modelo.Nombre.Trim(),
            Descripción = LimpiarOpcional(modelo.Descripcion),
            Talla = modelo.Talla.Trim(),
            Precio = modelo.Precio,
            Stock = modelo.Stock,
            CategoriaIdcategoria = modelo.CategoriaIdcategoria,
            Activo = true
        };

        try
        {
            if (modelo.ImagenArchivo is not null)
            {
                producto.Imagen = await _assetStore.SaveImageAsync(
                    modelo.ImagenArchivo,
                    HttpContext.RequestAborted);
            }

            await _context.Productos.AddAsync(producto);
            await _context.SaveChangesAsync();
        }
        catch (InvalidStorefrontImageException exception)
        {
            ModelState.AddModelError(nameof(modelo.ImagenArchivo), exception.Message);
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "No se pudo guardar la imagen del producto.");
            ModelState.AddModelError(string.Empty, "No fue posible guardar la imagen. Revise el almacenamiento configurado.");
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogError(exception, "No hay permisos para guardar la imagen del producto.");
            ModelState.AddModelError(string.Empty, "No hay permisos de escritura en el almacenamiento configurado.");
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "No se pudo registrar el producto.");
            ModelState.AddModelError(
                string.Empty,
                "No fue posible registrar el producto. Verifique los datos e inténtelo nuevamente.");
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }

        TempData["Success"] = "El producto se registró correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(producto => producto.Idproductos == id);

        if (producto is null)
        {
            return NotFound();
        }

        ViewData["ProductoId"] = id;
        ViewData["ImagenActual"] = producto.Imagen;
        await CargarCategoriasAsync(producto.CategoriaIdcategoria);
        return View(new ProductoFormViewModel
        {
            Nombre = producto.Nombre,
            Descripcion = producto.Descripción,
            Talla = producto.Talla,
            Precio = producto.Precio,
            Stock = producto.Stock,
            CategoriaIdcategoria = producto.CategoriaIdcategoria,
            ImagenArchivo = null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ProductoFormViewModel modelo)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(producto => producto.Idproductos == id);

        if (producto is null)
        {
            return NotFound();
        }

        ValidarPrecisionPrecio(modelo.Precio);
        if (!await CategoriaSeleccionableAsync(
                modelo.CategoriaIdcategoria,
                producto.CategoriaIdcategoria))
        {
            ModelState.AddModelError(
                nameof(modelo.CategoriaIdcategoria),
                "Seleccione una categoría activa.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["ProductoId"] = id;
            ViewData["ImagenActual"] = producto.Imagen;
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }

        try
        {
            if (modelo.QuitarImagen)
            {
                producto.Imagen = null;
            }

            if (modelo.ImagenArchivo is not null)
            {
                producto.Imagen = await _assetStore.SaveImageAsync(
                    modelo.ImagenArchivo,
                    HttpContext.RequestAborted);
            }
        }
        catch (InvalidStorefrontImageException exception)
        {
            ModelState.AddModelError(nameof(modelo.ImagenArchivo), exception.Message);
            ViewData["ProductoId"] = id;
            ViewData["ImagenActual"] = producto.Imagen;
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "No se pudo guardar la imagen del producto {ProductoId}.", id);
            ModelState.AddModelError(string.Empty, "No fue posible guardar la imagen. Revise el almacenamiento configurado.");
            ViewData["ProductoId"] = id;
            ViewData["ImagenActual"] = producto.Imagen;
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogError(exception, "No hay permisos para guardar la imagen del producto {ProductoId}.", id);
            ModelState.AddModelError(string.Empty, "No hay permisos de escritura en el almacenamiento configurado.");
            ViewData["ProductoId"] = id;
            ViewData["ImagenActual"] = producto.Imagen;
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }

        producto.Nombre = modelo.Nombre.Trim();
        producto.Descripción = LimpiarOpcional(modelo.Descripcion);
        producto.Talla = modelo.Talla.Trim();
        producto.Precio = modelo.Precio;
        producto.Stock = modelo.Stock;
        producto.CategoriaIdcategoria = modelo.CategoriaIdcategoria;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "No se pudo actualizar el producto {ProductoId}.", id);
            ModelState.AddModelError(
                string.Empty,
                "No fue posible actualizar el producto. Verifique los datos e inténtelo nuevamente.");
            ViewData["ProductoId"] = id;
            ViewData["ImagenActual"] = producto.Imagen;
            await CargarCategoriasAsync(modelo.CategoriaIdcategoria);
            return View(modelo);
        }

        TempData["Success"] = "El producto se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(producto => producto.Idproductos == id);

        if (producto is null)
        {
            return NotFound();
        }

        if (!producto.Activo)
        {
            TempData["Error"] = "El producto ya está desactivado.";
            return RedirectToAction(nameof(Index));
        }

        producto.Activo = false;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "No se pudo desactivar el producto {ProductoId}.", id);
            TempData["Error"] = "No fue posible desactivar el producto.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "El producto se desactivó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var producto = await _context.Productos
            .Include(producto => producto.CategoriaIdcategoriaNavigation)
            .FirstOrDefaultAsync(producto => producto.Idproductos == id);

        if (producto is null)
        {
            return NotFound();
        }

        if (producto.Activo)
        {
            TempData["Error"] = "El producto ya está activo.";
            return RedirectToAction(nameof(Index));
        }

        if (!producto.CategoriaIdcategoriaNavigation.Activo)
        {
            TempData["Error"] = "No se puede reactivar el producto porque su categoría está desactivada.";
            return RedirectToAction(nameof(Index));
        }

        producto.Activo = true;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "No se pudo reactivar el producto {ProductoId}.", id);
            TempData["Error"] = "No fue posible reactivar el producto.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "El producto se reactivó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CategoriaSeleccionableAsync(int categoriaId, int? categoriaActualId = null)
    {
        return await _context.Categoria.AnyAsync(categoria =>
            categoria.Idcategoria == categoriaId
            && (categoria.Activo
                || (categoriaActualId.HasValue && categoria.Idcategoria == categoriaActualId.Value)));
    }

    private async Task CargarCategoriasAsync(int? seleccionada = null)
    {
        var categorias = await _context.Categoria
            .AsNoTracking()
            .Where(categoria => categoria.Activo
                || (seleccionada.HasValue && categoria.Idcategoria == seleccionada.Value))
            .OrderBy(categoria => categoria.NombreCategoria)
            .ToListAsync();

        ViewBag.Categorias = new SelectList(
            categorias,
            nameof(Categorium.Idcategoria),
            nameof(Categorium.NombreCategoria),
            seleccionada);
    }

    private static string? LimpiarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private void ValidarPrecisionPrecio(decimal precio)
    {
        if (decimal.Round(precio, 2) != precio)
        {
            ModelState.AddModelError(
                nameof(ProductoFormViewModel.Precio),
                "El precio no puede tener más de dos decimales.");
        }
    }
}
