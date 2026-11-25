using HocicosAJ.Data;
using HocicosAJ.Models.ViewModels;
using HocicosAJ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Policy = "Administradores")]
public class AdminEstadisticasController : Controller
{
    private readonly EstadisticasDatosService _estadisticasDatos;

    public AdminEstadisticasController(EstadisticasDatosService estadisticasDatos)
    {
        _estadisticasDatos = estadisticasDatos;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AdminEstadisticasFiltrosViewModel filtros)
    {
        if (filtros.Desde.HasValue
            && filtros.Hasta.HasValue
            && filtros.Desde.Value > filtros.Hasta.Value)
        {
            ModelState.AddModelError(
                "Filtros.Hasta",
                "La fecha inicial no puede ser posterior a la fecha final.");
        }

        if (!ModelState.IsValid)
        {
            return View(new AdminEstadisticasViewModel { Filtros = filtros });
        }

        var datos = await _estadisticasDatos.ObtenerAsync(filtros, HttpContext.RequestAborted);

        var modelo = EstadisticasDescriptivas.Calcular(datos.Compras, datos.Detalles, filtros);
        modelo.AnalisisProbabilistico = AnalisisProbabilistico.Calcular(
            datos.Compras,
            datos.Detalles,
            filtros,
            modelo);
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Graficas(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("El período indicado no es válido.");
        }

        if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
        {
            return BadRequest("La fecha inicial no puede ser posterior a la fecha final.");
        }

        var filtros = new AdminEstadisticasFiltrosViewModel
        {
            Desde = desde,
            Hasta = hasta
        };
        var datos = await _estadisticasDatos.ObtenerAsync(filtros, cancellationToken);
        var frecuenciasImportes = EstadisticasDescriptivas.Calcular(
            datos.Compras,
            datos.Detalles,
            filtros).FrecuenciasImportes;
        var graficas = await _estadisticasDatos.ObtenerGraficasAsync(
            datos,
            frecuenciasImportes,
            desde,
            hasta,
            cancellationToken);

        return Json(graficas);
    }
}
