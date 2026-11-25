using HocicosAJ.Data;
using HocicosAJ.Models.ViewModels;
using HocicosAJ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Policy = "Administradores")]
public sealed class AdminDashboardController(
    HocicosAjContext context,
    EstadisticasDatosService estadisticasDatos,
    StorefrontSettingsStore settingsStore) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var storefrontSettings = await settingsStore.GetAsync(cancellationToken);
        ViewData["DashboardImagePath"] = storefrontSettings.Settings.DashboardImagePath;

        var inicio = DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var filtros = new AdminEstadisticasFiltrosViewModel
        {
            Desde = inicio,
            Hasta = hoy,
            Percentil = 90
        };
        var datosEstadisticos = await estadisticasDatos.ObtenerAsync(filtros, cancellationToken);
        var estadisticas = EstadisticasDescriptivas.Calcular(
            datosEstadisticos.Compras,
            datosEstadisticos.Detalles,
            filtros);
        estadisticas.AnalisisProbabilistico = AnalisisProbabilistico.Calcular(
            datosEstadisticos.Compras,
            datosEstadisticos.Detalles,
            filtros,
            estadisticas);

        var ultimoMes = DateTime.Today.AddDays(-29);
        var importeRegistrado = await context.Compras
            .AsNoTracking()
            .Where(compra => compra.Fecha >= ultimoMes)
            .Select(compra => (decimal?)compra.Total)
            .SumAsync(cancellationToken) ?? 0m;
        var comprasUltimos30Dias = await context.Compras
            .AsNoTracking()
            .CountAsync(compra => compra.Fecha >= ultimoMes, cancellationToken);
        var inventarioBajo = await context.Productos
            .AsNoTracking()
            .Where(producto =>
                producto.Activo
                && producto.Stock > 0
                && producto.CategoriaIdcategoriaNavigation.Activo)
            .OrderBy(producto => producto.Stock)
            .ThenBy(producto => producto.Nombre)
            .Take(5)
            .Select(producto => new ProductoStockViewModel
            {
                Id = producto.Idproductos,
                Nombre = producto.Nombre,
                Categoria = producto.CategoriaIdcategoriaNavigation.NombreCategoria,
                Stock = producto.Stock
            })
            .ToListAsync(cancellationToken);

        return View(new AdminDashboardViewModel
        {
            ProductosActivos = await context.Productos.AsNoTracking().CountAsync(
                producto => producto.Activo && producto.CategoriaIdcategoriaNavigation.Activo,
                cancellationToken),
            ProductosSinStock = await context.Productos.AsNoTracking().CountAsync(
                producto =>
                    producto.Activo
                    && producto.Stock == 0
                    && producto.CategoriaIdcategoriaNavigation.Activo,
                cancellationToken),
            CategoriasActivas = await context.Categoria.AsNoTracking().CountAsync(
                categoria => categoria.Activo,
                cancellationToken),
            UsuariosRegistrados = await context.Usuarios.AsNoTracking().CountAsync(cancellationToken),
            ComprasUltimos30Dias = comprasUltimos30Dias,
            ImporteRegistradoUltimos30Dias = importeRegistrado,
            InventarioBajo = inventarioBajo,
            Estadisticas = estadisticas
        });
    }
}
