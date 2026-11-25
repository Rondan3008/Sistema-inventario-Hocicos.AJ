using System.Security.Claims;
using HocicosAJ.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Roles = "Cliente")]
public class ComprobantesController : Controller
{
    private readonly HocicosAjContext _context;

    public ComprobantesController(HocicosAjContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? compraId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
        {
            return Forbid();
        }

        var consulta = _context.Comprobantes
            .AsNoTracking()
            .Include(comprobante => comprobante.ComprasIdcomprasNavigation)
            .Where(comprobante =>
                comprobante.ComprasIdcomprasNavigation.ClienteIdusuarios == clienteId);

        if (compraId.HasValue)
        {
            consulta = consulta.Where(comprobante => comprobante.ComprasIdcompras == compraId.Value);
        }

        var comprobantes = await consulta
            .OrderByDescending(comprobante => comprobante.FechaEmision)
            .ToListAsync();

        ViewData["CompraId"] = compraId;
        return View(comprobantes);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clienteId))
        {
            return Forbid();
        }

        var comprobante = await _context.Comprobantes
            .AsNoTracking()
            .Include(registro => registro.ComprasIdcomprasNavigation)
            .FirstOrDefaultAsync(registro =>
                registro.Idcomprobante == id
                && registro.ComprasIdcomprasNavigation.ClienteIdusuarios == clienteId);

        return comprobante is null ? NotFound() : View(comprobante);
    }
}
