using HocicosAJ.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Policy = "Administradores")]
public class AdminComprobantesController : Controller
{
    private readonly HocicosAjContext _context;

    public AdminComprobantesController(HocicosAjContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? compraId)
    {
        var consulta = _context.Comprobantes
            .AsNoTracking()
            .Include(comprobante => comprobante.ComprasIdcomprasNavigation)
                .ThenInclude(compra => compra.ClienteIdusuariosNavigation)
            .AsQueryable();

        if (compraId.HasValue)
        {
            consulta = consulta.Where(comprobante =>
                comprobante.ComprasIdcompras == compraId.Value);
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
        var comprobante = await _context.Comprobantes
            .AsNoTracking()
            .Include(registro => registro.ComprasIdcomprasNavigation)
                .ThenInclude(compra => compra.ClienteIdusuariosNavigation)
            .FirstOrDefaultAsync(registro => registro.Idcomprobante == id);

        return comprobante is null ? NotFound() : View(comprobante);
    }
}
