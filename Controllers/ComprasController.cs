using System.Data;
using System.Data.Common;
using System.Security.Claims;
using HocicosAJ.Data;
using HocicosAJ.Models.Database;
using HocicosAJ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;

namespace HocicosAJ.Controllers;

[Authorize(Roles = "Cliente")]
public class ComprasController : Controller
{
    private const decimal MaximoTotal = 99999999.99m;
    private const string EstadoPendiente = "Pendiente";

    private readonly HocicosAjContext _context;
    private readonly ILogger<ComprasController> _logger;

    public ComprasController(
        HocicosAjContext context,
        ILogger<ComprasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(string? claveIdempotencia)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
        {
            return Forbid();
        }

        if (!Guid.TryParseExact(claveIdempotencia, "D", out var claveGuid)
            || claveGuid == Guid.Empty)
        {
            TempData["Error"] = "La solicitud de compra no es válida. Revise el carrito e inténtelo nuevamente.";
            return RedirectToAction("Index", "Carrito");
        }

        var clave = claveGuid.ToString("D");
        var compraExistente = await _context.Compras
            .AsNoTracking()
            .Where(compra =>
                compra.ClaveIdempotencia == clave
                && compra.ClienteIdusuarios == clienteId)
            .Select(compra => (int?)compra.Idcompras)
            .SingleOrDefaultAsync();
        if (compraExistente.HasValue)
        {
            TempData["Success"] = "Esta compra ya había sido confirmada.";
            return RedirectToAction(nameof(Detalle), new { id = compraExistente.Value });
        }

        var carrito = CarritoSesion.Leer(HttpContext.Session);
        if (carrito.Count == 0)
        {
            TempData["Error"] = "El carrito está vacío.";
            return RedirectToAction("Index", "Carrito");
        }

        if (!CarritoSesion.ClaveCheckoutVigente(HttpContext.Session, carrito, claveGuid))
        {
            TempData["Error"] = "El carrito cambió. Revíselo y confirme nuevamente.";
            return RedirectToAction("Index", "Carrito");
        }

        IDbContextTransaction? transaccionActiva = null;
        try
        {
            var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            transaccionActiva = transaccion;

            var compra = new Compra
            {
                Fecha = DateTime.Now,
                Total = 0m,
                Estado = EstadoPendiente,
                ClaveIdempotencia = clave,
                UsuariosIdusuarios = clienteId,
                ClienteIdusuarios = clienteId
            };
            await _context.Compras.AddAsync(compra);
            await _context.SaveChangesAsync();

            var productoIds = carrito.Keys.Order().ToList();
            var productos = await _context.Productos
                .AsNoTracking()
                .Include(producto => producto.CategoriaIdcategoriaNavigation)
                .Where(producto => productoIds.Contains(producto.Idproductos))
                .ToDictionaryAsync(producto => producto.Idproductos);

            if (productos.Count != carrito.Count)
            {
                await transaccion.RollbackAsync();
                TempData["Error"] = "Uno o más productos del carrito ya no existen. Revise su carrito.";
                return RedirectToAction("Index", "Carrito");
            }

            decimal total = 0m;
            foreach (var (productoId, cantidad) in carrito)
            {
                var producto = productos[productoId];
                if (!producto.Activo
                    || !producto.CategoriaIdcategoriaNavigation.Activo
                    || cantidad <= 0
                    || producto.Stock < cantidad)
                {
                    await transaccion.RollbackAsync();
                    TempData["Error"] =
                        $"La disponibilidad de «{producto.Nombre}» cambió. Revise su carrito.";
                    return RedirectToAction("Index", "Carrito");
                }

                total += producto.Precio * cantidad;
                if (total > MaximoTotal)
                {
                    await transaccion.RollbackAsync();
                    TempData["Error"] = "El total de la compra supera el límite admitido.";
                    return RedirectToAction("Index", "Carrito");
                }
            }

            compra.Total = total;
            await _context.SaveChangesAsync();

            foreach (var (productoId, cantidad) in carrito.OrderBy(linea => linea.Key))
            {
                var precioActual = productos[productoId].Precio;
                var actualizado = await _context.Productos
                    .Where(producto =>
                        producto.Idproductos == productoId
                        && producto.Activo
                        && producto.Precio == precioActual
                        && producto.Stock >= cantidad
                        && producto.CategoriaIdcategoriaNavigation.Activo)
                    .ExecuteUpdateAsync(actualizacion => actualizacion
                        .SetProperty(producto => producto.Stock, producto => producto.Stock - cantidad));

                if (actualizado != 1)
                {
                    await transaccion.RollbackAsync();
                    TempData["Error"] =
                        "El precio o la disponibilidad de un producto cambió. Revise su carrito.";
                    return RedirectToAction("Index", "Carrito");
                }
            }

            var detalles = carrito.Select(linea =>
            {
                var producto = productos[linea.Key];
                return new DetalleCompra
                {
                    Cantidad = linea.Value,
                    PrecioUnitario = producto.Precio,
                    Subtotal = producto.Precio * linea.Value,
                    ProductosIdproductos = producto.Idproductos,
                    ComprasIdcompras = compra.Idcompras
                };
            });

            await _context.DetalleCompras.AddRangeAsync(detalles);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            CarritoSesion.VaciarSiCoincide(HttpContext.Session, carrito, claveGuid);
            TempData["Success"] = $"La compra #{compra.Idcompras} se confirmó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = compra.Idcompras });
        }
        catch (DbUpdateException exception) when (EsConflictoClaveIdempotencia(exception))
        {
            if (transaccionActiva is not null)
            {
                await transaccionActiva.RollbackAsync();
                await transaccionActiva.DisposeAsync();
                transaccionActiva = null;
            }

            var compraConfirmada = await _context.Compras
                .AsNoTracking()
                .Where(compra => compra.ClaveIdempotencia == clave)
                .Select(compra => new { compra.Idcompras, compra.ClienteIdusuarios })
                .SingleOrDefaultAsync();

            if (compraConfirmada is null || compraConfirmada.ClienteIdusuarios != clienteId)
            {
                TempData["Error"] =
                    "No fue posible confirmar la compra. Revise el carrito e inténtelo nuevamente con la misma operación.";
                return RedirectToAction("Index", "Carrito");
            }

            TempData["Success"] = "Esta compra ya había sido confirmada.";
            return RedirectToAction(nameof(Detalle), new { id = compraConfirmada.Idcompras });
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            if (transaccionActiva is not null)
            {
                await transaccionActiva.RollbackAsync();
            }

            _logger.LogError(exception, "No se pudo confirmar la compra del cliente {ClienteId}.", clienteId);
            TempData["Error"] =
                "No fue posible confirmar la compra. Revise el carrito e inténtelo nuevamente con la misma operación.";
            return RedirectToAction("Index", "Carrito");
        }
        finally
        {
            if (transaccionActiva is not null)
            {
                await transaccionActiva.DisposeAsync();
            }
        }
    }

    private static bool EsConflictoClaveIdempotencia(DbUpdateException exception)
    {
        return exception.GetBaseException() is MySqlException mysqlException
            && mysqlException.Number == 1062
            && mysqlException.Message.Contains(
                "uq_compras_clave_idempotencia",
                StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> Historial()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
        {
            return Forbid();
        }

        var compras = await _context.Compras
            .AsNoTracking()
            .Where(compra => compra.ClienteIdusuarios == clienteId)
            .OrderByDescending(compra => compra.Fecha)
            .ToListAsync();

        return View(compras);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
        {
            return Forbid();
        }

        var compra = await _context.Compras
            .AsNoTracking()
            .Include(compra => compra.DetalleCompras)
                .ThenInclude(detalle => detalle.ProductosIdproductosNavigation)
            .FirstOrDefaultAsync(compra =>
                compra.Idcompras == id && compra.ClienteIdusuarios == clienteId);

        if (compra is null)
        {
            return NotFound();
        }

        return View(compra);
    }
}
