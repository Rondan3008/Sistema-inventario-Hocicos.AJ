using HocicosAJ.Data;
using HocicosAJ.Models.Database;
using HocicosAJ.Models.ViewModels;
using HocicosAJ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Roles = "Cliente")]
public class CarritoController : Controller
{
    private readonly HocicosAjContext _context;

    public CarritoController(HocicosAjContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var carrito = CarritoSesion.Leer(HttpContext.Session);
        if (carrito.Count == 0)
        {
            return View(new CarritoViewModel());
        }

        var ids = carrito.Keys.ToList();
        var productos = await _context.Productos
            .AsNoTracking()
            .Include(producto => producto.CategoriaIdcategoriaNavigation)
            .Where(producto => ids.Contains(producto.Idproductos))
            .ToDictionaryAsync(producto => producto.Idproductos);

        var lineas = new List<CarritoLineaViewModel>(carrito.Count);
        foreach (var (productoId, cantidad) in carrito)
        {
            productos.TryGetValue(productoId, out var producto);
            var puedeActualizar = producto is not null
                && producto.Activo
                && producto.CategoriaIdcategoriaNavigation.Activo
                && producto.Stock > 0;
            lineas.Add(new CarritoLineaViewModel
            {
                ProductoId = productoId,
                Producto = producto,
                Cantidad = cantidad,
                PuedeActualizar = puedeActualizar,
                Disponible = puedeActualizar && producto!.Stock >= cantidad
            });
        }

        var modelo = new CarritoViewModel
        {
            Lineas = lineas,
            Total = lineas.Sum(linea => linea.Subtotal)
        };
        if (modelo.PuedeConfirmar)
        {
            modelo.ClaveIdempotencia =
                CarritoSesion.ObtenerClaveCheckout(HttpContext.Session, carrito);
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int productoId, int cantidad = 1)
    {
        if (productoId <= 0 || cantidad <= 0)
        {
            TempData["Error"] = "La cantidad solicitada no es válida.";
            return RedirectToAction("Index", "Productos");
        }

        var producto = await _context.Productos
            .AsNoTracking()
            .Include(producto => producto.CategoriaIdcategoriaNavigation)
            .FirstOrDefaultAsync(producto => producto.Idproductos == productoId);

        if (producto is null
            || !producto.Activo
            || !producto.CategoriaIdcategoriaNavigation.Activo)
        {
            TempData["Error"] = "El producto ya no está disponible.";
            return RedirectToAction("Index", "Productos");
        }

        var carrito = CarritoSesion.Leer(HttpContext.Session);
        var cantidadActual = carrito.GetValueOrDefault(productoId);
        if (cantidad > producto.Stock - cantidadActual)
        {
            TempData["Error"] = "La cantidad solicitada supera el stock disponible.";
            return RedirectToAction("Index", "Productos");
        }

        carrito[productoId] = cantidadActual + cantidad;
        CarritoSesion.Guardar(HttpContext.Session, carrito);
        TempData["Success"] = "El producto se agregó al carrito.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int productoId, int cantidad)
    {
        var carrito = CarritoSesion.Leer(HttpContext.Session);
        if (!carrito.ContainsKey(productoId))
        {
            TempData["Error"] = "El producto ya no está en el carrito.";
            return RedirectToAction(nameof(Index));
        }

        if (cantidad <= 0)
        {
            TempData["Error"] = "La cantidad debe ser mayor que cero.";
            return RedirectToAction(nameof(Index));
        }

        var producto = await _context.Productos
            .AsNoTracking()
            .Include(producto => producto.CategoriaIdcategoriaNavigation)
            .FirstOrDefaultAsync(producto => producto.Idproductos == productoId);

        if (producto is null
            || !producto.Activo
            || !producto.CategoriaIdcategoriaNavigation.Activo
            || cantidad > producto.Stock)
        {
            TempData["Error"] = "La cantidad solicitada supera la disponibilidad actual.";
            return RedirectToAction(nameof(Index));
        }

        carrito[productoId] = cantidad;
        CarritoSesion.Guardar(HttpContext.Session, carrito);
        TempData["Success"] = "La cantidad se actualizó.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Retirar(int productoId)
    {
        var carrito = CarritoSesion.Leer(HttpContext.Session);
        if (!carrito.Remove(productoId))
        {
            TempData["Error"] = "El producto ya no está en el carrito.";
            return RedirectToAction(nameof(Index));
        }

        CarritoSesion.Guardar(HttpContext.Session, carrito);
        TempData["Success"] = "El producto se retiró del carrito.";
        return RedirectToAction(nameof(Index));
    }
}
