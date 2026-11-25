using System.Globalization;
using HocicosAJ.Data;
using HocicosAJ.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Services;

internal sealed record EstadisticasDatos(
    IReadOnlyList<CompraEstadisticaRegistro> Compras,
    IReadOnlyList<DetalleEstadisticaRegistro> Detalles);

public sealed class EstadisticasDatosService(HocicosAjContext context)
{
    internal async Task<EstadisticasDatos> ObtenerAsync(
        AdminEstadisticasFiltrosViewModel filtros,
        CancellationToken cancellationToken = default)
    {
        var consultaCompras = context.Compras.AsNoTracking();
        if (filtros.Desde.HasValue)
        {
            var fechaInicial = filtros.Desde.Value.ToDateTime(TimeOnly.MinValue);
            consultaCompras = consultaCompras.Where(compra => compra.Fecha >= fechaInicial);
        }

        if (filtros.Hasta.HasValue && filtros.Hasta.Value != DateOnly.MaxValue)
        {
            var fechaFinalExclusiva = filtros.Hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            consultaCompras = consultaCompras.Where(compra => compra.Fecha < fechaFinalExclusiva);
        }

        var registrosCompra = await consultaCompras
            .Select(compra => new
            {
                compra.Idcompras,
                compra.Fecha,
                compra.Total
            })
            .ToListAsync(cancellationToken);
        var compras = registrosCompra
            .Select(compra => new CompraEstadisticaRegistro(
                compra.Idcompras,
                compra.Fecha,
                compra.Total))
            .ToArray();

        if (compras.Length == 0)
        {
            return new EstadisticasDatos(compras, []);
        }

        var comprasIds = compras.Select(compra => compra.Id).ToList();
        var registrosDetalle = await context.DetalleCompras
            .AsNoTracking()
            .Where(detalle => comprasIds.Contains(detalle.ComprasIdcompras))
            .Select(detalle => new
            {
                detalle.ComprasIdcompras,
                detalle.ProductosIdproductos,
                detalle.ProductosIdproductosNavigation.Nombre,
                detalle.ProductosIdproductosNavigation.CategoriaIdcategoria,
                detalle.ProductosIdproductosNavigation.CategoriaIdcategoriaNavigation.NombreCategoria,
                detalle.Cantidad
            })
            .ToListAsync(cancellationToken);
        var detalles = registrosDetalle
            .Select(detalle => new DetalleEstadisticaRegistro(
                detalle.ComprasIdcompras,
                detalle.ProductosIdproductos,
                detalle.Nombre,
                detalle.CategoriaIdcategoria,
                detalle.NombreCategoria,
                detalle.Cantidad))
            .ToArray();

        return new EstadisticasDatos(compras, detalles);
    }

    internal async Task<AdminEstadisticasGraficasViewModel> ObtenerGraficasAsync(
        EstadisticasDatos datos,
        IReadOnlyList<FrecuenciaImporteViewModel> frecuenciasImportes,
        DateOnly? desde,
        DateOnly? hasta,
        CancellationToken cancellationToken = default)
    {
        var histograma = frecuenciasImportes
            .Select(frecuencia => new DatoGraficaEstadisticaViewModel
            {
                Etiqueta = frecuencia.Intervalo,
                Valor = frecuencia.Frecuencia
            })
            .ToArray();

        if (datos.Compras.Count == 0)
        {
            return new AdminEstadisticasGraficasViewModel
            {
                HistogramaImportes = histograma
            };
        }

        var comprasIds = datos.Compras.Select(compra => compra.Id).ToArray();
        var registrosDetalle = await context.DetalleCompras
            .AsNoTracking()
            .Where(detalle => comprasIds.Contains(detalle.ComprasIdcompras))
            .Select(detalle => new
            {
                detalle.ProductosIdproductos,
                detalle.ProductosIdproductosNavigation.Nombre,
                detalle.ProductosIdproductosNavigation.CategoriaIdcategoria,
                detalle.ProductosIdproductosNavigation.CategoriaIdcategoriaNavigation.NombreCategoria,
                detalle.Cantidad,
                detalle.Subtotal
            })
            .ToListAsync(cancellationToken);

        var unidadesPorProducto = registrosDetalle
            .GroupBy(detalle => detalle.ProductosIdproductos)
            .Select(grupo => new
            {
                grupo.Key,
                Nombre = grupo.First().Nombre,
                Unidades = grupo.Sum(detalle => (long)detalle.Cantidad)
            })
            .OrderByDescending(producto => producto.Unidades)
            .ThenBy(producto => producto.Nombre)
            .Select(producto => new DatoGraficaEstadisticaViewModel
            {
                Etiqueta = $"{producto.Nombre} (ID {producto.Key})",
                Valor = producto.Unidades
            })
            .ToArray();

        var importesPorProducto = registrosDetalle
            .GroupBy(detalle => detalle.ProductosIdproductos)
            .Select(grupo => new
            {
                grupo.Key,
                Nombre = grupo.First().Nombre,
                Importe = grupo.Sum(detalle => detalle.Subtotal)
            })
            .OrderByDescending(producto => producto.Importe)
            .ThenBy(producto => producto.Nombre)
            .Select(producto => new DatoGraficaEstadisticaViewModel
            {
                Etiqueta = $"{producto.Nombre} (ID {producto.Key})",
                Valor = producto.Importe
            })
            .ToArray();

        var importesCategoria = registrosDetalle
            .GroupBy(detalle => detalle.CategoriaIdcategoria)
            .Select(grupo => new
            {
                Id = grupo.Key,
                Nombre = grupo.First().NombreCategoria,
                Importe = grupo.Sum(detalle => detalle.Subtotal)
            })
            .OrderByDescending(categoria => categoria.Importe)
            .ThenBy(categoria => categoria.Nombre)
            .ThenBy(categoria => categoria.Id)
            .ToArray();
        var importeTotalDetalle = importesCategoria.Sum(categoria => categoria.Importe);
        var participacionPorCategoria = importeTotalDetalle > 0
            && importesCategoria.All(categoria => categoria.Importe >= 0)
                ? importesCategoria
                    .Select(categoria => new DatoGraficaEstadisticaViewModel
                    {
                        Etiqueta = $"{categoria.Nombre} (ID {categoria.Id})",
                        Valor = categoria.Importe
                    })
                    .ToArray()
                : [];

        var comprobantesPorMetodo = await context.Comprobantes
            .AsNoTracking()
            .Where(comprobante => comprasIds.Contains(comprobante.ComprasIdcompras))
            .GroupBy(comprobante => comprobante.MetodoPago)
            .Select(grupo => new
            {
                MetodoPago = grupo.Key,
                Frecuencia = grupo.Count()
            })
            .OrderByDescending(grupo => grupo.Frecuencia)
            .ThenBy(grupo => grupo.MetodoPago)
            .ToListAsync(cancellationToken);

        var esDiaria = desde.HasValue
            && hasta.HasValue
            && hasta.Value.DayNumber - desde.Value.DayNumber <= 30;
        var comprasPorFecha = datos.Compras
            .GroupBy(compra => esDiaria
                ? DateOnly.FromDateTime(compra.Fecha)
                : new DateOnly(compra.Fecha.Year, compra.Fecha.Month, 1))
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new DatoGraficaEstadisticaViewModel
            {
                Etiqueta = grupo.Key.ToString(
                    esDiaria ? "dd/MM/yyyy" : "MM/yyyy",
                    CultureInfo.CurrentCulture),
                Valor = grupo.Count()
            })
            .ToArray();

        return new AdminEstadisticasGraficasViewModel
        {
            UnidadesPorProducto = unidadesPorProducto,
            ImportePorProducto = importesPorProducto,
            ParticipacionPorCategoria = participacionPorCategoria,
            HistogramaImportes = histograma,
            ComprasPorFecha = comprasPorFecha,
            MetodosPago = comprobantesPorMetodo
                .Select(grupo => new DatoGraficaEstadisticaViewModel
                {
                    Etiqueta = grupo.MetodoPago,
                    Valor = grupo.Frecuencia
                })
                .ToArray()
        };
    }
}
