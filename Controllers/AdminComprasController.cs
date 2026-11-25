using HocicosAJ.Data;
using HocicosAJ.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Policy = "Administradores")]
public class AdminComprasController : Controller
{
    private readonly HocicosAjContext _context;

    public AdminComprasController(HocicosAjContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AdminComprasFiltrosViewModel filtros)
    {
        if (filtros.Desde.HasValue
            && filtros.Hasta.HasValue
            && filtros.Desde.Value > filtros.Hasta.Value)
        {
            ModelState.AddModelError(
                "Filtros.Hasta",
                "La fecha inicial no puede ser posterior a la fecha final.");
        }

        var modelo = new AdminComprasIndexViewModel { Filtros = filtros };
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var consulta = _context.Compras.AsNoTracking();
        if (filtros.Desde.HasValue)
        {
            var fechaInicial = filtros.Desde.Value.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(compra => compra.Fecha >= fechaInicial);
        }

        if (filtros.Hasta.HasValue && filtros.Hasta.Value != DateOnly.MaxValue)
        {
            var fechaFinalExclusiva = filtros.Hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(compra => compra.Fecha < fechaFinalExclusiva);
        }

        var cliente = filtros.Cliente?.Trim();
        if (!string.IsNullOrEmpty(cliente))
        {
            filtros.Cliente = cliente;
            consulta = consulta.Where(compra =>
                compra.ClienteIdusuariosNavigation.Nombre.Contains(cliente)
                || (compra.ClienteIdusuariosNavigation.Usuario1 != null
                    && compra.ClienteIdusuariosNavigation.Usuario1.Contains(cliente)));
        }

        modelo.Compras = await consulta
            .OrderByDescending(compra => compra.Fecha)
            .ThenByDescending(compra => compra.Idcompras)
            .Select(compra => new AdminCompraResumenViewModel
            {
                Id = compra.Idcompras,
                Fecha = compra.Fecha,
                Estado = compra.Estado,
                Total = compra.Total,
                ClienteNombre = compra.ClienteIdusuariosNavigation.Nombre,
                ClienteUsuario = compra.ClienteIdusuariosNavigation.Usuario1
            })
            .ToListAsync();

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var compra = await _context.Compras
            .AsNoTracking()
            .Where(compra => compra.Idcompras == id)
            .Select(compra => new AdminCompraDetalleViewModel
            {
                Id = compra.Idcompras,
                Fecha = compra.Fecha,
                Estado = compra.Estado,
                Total = compra.Total,
                ClienteNombre = compra.ClienteIdusuariosNavigation.Nombre,
                ClienteUsuario = compra.ClienteIdusuariosNavigation.Usuario1,
                Detalles = compra.DetalleCompras
                    .OrderBy(detalle => detalle.Iddetalle)
                    .Select(detalle => new AdminCompraLineaViewModel
                    {
                        Producto = detalle.ProductosIdproductosNavigation.Nombre,
                        Talla = detalle.ProductosIdproductosNavigation.Talla,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario,
                        Subtotal = detalle.Subtotal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (compra is null)
        {
            return NotFound();
        }

        return View(compra);
    }
}
