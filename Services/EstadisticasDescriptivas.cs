using System.Globalization;
using HocicosAJ.Models.ViewModels;

namespace HocicosAJ.Services;

internal sealed record CompraEstadisticaRegistro(int Id, DateTime Fecha, decimal Total);

internal sealed record DetalleEstadisticaRegistro(
    int CompraId,
    int ProductoId,
    string NombreProducto,
    int CategoriaId,
    string NombreCategoria,
    int Cantidad);

internal static class EstadisticasDescriptivas
{
    public static AdminEstadisticasViewModel Calcular(
        IReadOnlyCollection<CompraEstadisticaRegistro> compras,
        IReadOnlyCollection<DetalleEstadisticaRegistro> detalles,
        AdminEstadisticasFiltrosViewModel filtros)
    {
        var modelo = new AdminEstadisticasViewModel
        {
            Filtros = filtros,
            NumeroCompras = compras.Count,
            TieneDetalles = detalles.Count > 0
        };

        CalcularImportes(compras, modelo, filtros.Percentil);
        CalcularProductosYCategorias(detalles, modelo);
        modelo.VentasPorPeriodo = CrearBarrasPorPeriodo(
            compras,
            filtros.Desde,
            filtros.Hasta,
            out var granularidad);
        modelo.GranularidadVentas = granularidad;
        modelo.VentasPorCategoria = CrearBarrasPorCategoria(detalles);
        return modelo;
    }

    private static void CalcularImportes(
        IReadOnlyCollection<CompraEstadisticaRegistro> compras,
        AdminEstadisticasViewModel modelo,
        int percentil)
    {
        if (compras.Count == 0)
        {
            return;
        }

        var importes = compras.Select(compra => compra.Total).Order().ToArray();
        var total = importes.Sum();
        var media = total / importes.Length;
        modelo.TotalIngresos = total;
        modelo.ImportePromedio = media;
        modelo.Media = media;
        modelo.Mediana = importes.Length % 2 == 0
            ? (importes[importes.Length / 2 - 1] + importes[importes.Length / 2]) / 2m
            : importes[importes.Length / 2];
        modelo.Rango = importes[^1] - importes[0];
        modelo.PrimerCuartil = PercentilLineal(importes, 0.25m);
        modelo.TercerCuartil = PercentilLineal(importes, 0.75m);
        modelo.PercentilSeleccionado = PercentilLineal(importes, percentil / 100m);
        modelo.DiagramaCaja = CrearDiagramaCaja(
            importes,
            modelo.PrimerCuartil.Value,
            modelo.TercerCuartil.Value);

        var frecuencias = importes
            .GroupBy(importe => importe)
            .Select(grupo => new { Importe = grupo.Key, Frecuencia = grupo.Count() })
            .ToArray();
        var frecuenciaMaxima = frecuencias.Max(grupo => grupo.Frecuencia);
        if (frecuenciaMaxima > 1)
        {
            modelo.Modas = frecuencias
                .Where(grupo => grupo.Frecuencia == frecuenciaMaxima)
                .Select(grupo => grupo.Importe)
                .ToArray();
        }

        var mediaDouble = (double)media;
        var desviaciones = importes.Select(importe => (double)importe - mediaDouble).ToArray();
        var sumaCuadrados = desviaciones.Sum(desviacion => Math.Pow(desviacion, 2));
        var varianzaPoblacional = sumaCuadrados / importes.Length;
        modelo.VarianzaPoblacional = varianzaPoblacional;
        modelo.DesviacionEstandarPoblacional = Math.Sqrt(varianzaPoblacional);
        var segundoMomento = varianzaPoblacional;
        if (segundoMomento > 0)
        {
            var tercerMomento = desviaciones.Sum(desviacion => Math.Pow(desviacion, 3))
                / importes.Length;
            var cuartoMomento = desviaciones.Sum(desviacion => Math.Pow(desviacion, 4))
                / importes.Length;
            modelo.AsimetriaPoblacional = tercerMomento / Math.Pow(segundoMomento, 1.5);
            modelo.CurtosisExcesoPoblacional =
                cuartoMomento / Math.Pow(segundoMomento, 2) - 3;
        }

        if (importes.Length > 1)
        {
            var varianzaMuestral = sumaCuadrados / (importes.Length - 1);
            modelo.VarianzaMuestral = varianzaMuestral;
            modelo.DesviacionEstandarMuestral = Math.Sqrt(varianzaMuestral);
        }

        if (mediaDouble != 0)
        {
            modelo.CoeficienteVariacionPorcentual =
                modelo.DesviacionEstandarPoblacional.Value / Math.Abs(mediaDouble) * 100;
        }

        modelo.FrecuenciasImportes = CrearTablaFrecuencias(importes);
    }

    private static void CalcularProductosYCategorias(
        IReadOnlyCollection<DetalleEstadisticaRegistro> detalles,
        AdminEstadisticasViewModel modelo)
    {
        if (detalles.Count == 0)
        {
            return;
        }

        modelo.UnidadesVendidas = detalles.Sum(detalle => (long)detalle.Cantidad);

        var productos = detalles
            .GroupBy(detalle => detalle.ProductoId)
            .Select(grupo => new
            {
                Id = grupo.Key,
                Nombre = grupo.First().NombreProducto,
                Unidades = grupo.Sum(detalle => (long)detalle.Cantidad)
            })
            .ToArray();
        var maximoProducto = productos.Max(producto => producto.Unidades);
        modelo.UnidadesProductoMasVendido = maximoProducto;
        modelo.ProductoMasVendido = string.Join(
            ", ",
            productos
                .Where(producto => producto.Unidades == maximoProducto)
                .Select(producto => $"{producto.Nombre} (ID {producto.Id})"));

        var categorias = detalles
            .GroupBy(detalle => detalle.CategoriaId)
            .Select(grupo => new
            {
                Id = grupo.Key,
                Nombre = grupo.First().NombreCategoria,
                Unidades = grupo.Sum(detalle => (long)detalle.Cantidad)
            })
            .ToArray();
        var maximoCategoria = categorias.Max(categoria => categoria.Unidades);
        modelo.UnidadesCategoriaMasVendida = maximoCategoria;
        modelo.CategoriaMasVendida = string.Join(
            ", ",
            categorias
                .Where(categoria => categoria.Unidades == maximoCategoria)
                .Select(categoria => $"{categoria.Nombre} (ID {categoria.Id})"));
    }

    private static IReadOnlyList<FrecuenciaImporteViewModel> CrearTablaFrecuencias(
        decimal[] importes)
    {
        var minimo = importes[0];
        var maximo = importes[^1];
        var rango = maximo - minimo;
        var numeroClases = rango == 0
            ? 1
            : (int)Math.Ceiling(1 + Math.Log2(importes.Length));
        var amplitud = numeroClases == 1 ? 0 : rango / numeroClases;
        var frecuencias = new int[numeroClases];

        foreach (var importe in importes)
        {
            var indice = amplitud == 0
                ? 0
                : (int)decimal.Floor((importe - minimo) / amplitud);
            frecuencias[Math.Clamp(indice, 0, numeroClases - 1)]++;
        }

        var acumulada = 0;
        var frecuenciaMaxima = frecuencias.Max();
        var filas = new List<FrecuenciaImporteViewModel>(numeroClases);
        for (var indice = 0; indice < numeroClases; indice++)
        {
            acumulada += frecuencias[indice];
            var limiteInferior = minimo + amplitud * indice;
            var limiteSuperior = indice == numeroClases - 1
                ? maximo
                : minimo + amplitud * (indice + 1);
            var intervalo = numeroClases == 1 && rango == 0
                ? FormatearImporte(minimo)
                : indice == numeroClases - 1
                    ? $"[{FormatearImporte(limiteInferior)}, {FormatearImporte(limiteSuperior)}]"
                    : $"[{FormatearImporte(limiteInferior)}, {FormatearImporte(limiteSuperior)})";

            filas.Add(new FrecuenciaImporteViewModel
            {
                Intervalo = intervalo,
                Frecuencia = frecuencias[indice],
                FrecuenciaAcumulada = acumulada,
                FrecuenciaRelativaPorcentual = (double)frecuencias[indice] / importes.Length * 100,
                AlturaPixels = frecuenciaMaxima == 0
                    ? 0
                    : Math.Max(2, (int)Math.Round((double)frecuencias[indice] / frecuenciaMaxima * 180))
            });
        }

        return filas;
    }

    private static IReadOnlyList<BarraEstadisticaViewModel> CrearBarrasPorPeriodo(
        IReadOnlyCollection<CompraEstadisticaRegistro> compras,
        DateOnly? desde,
        DateOnly? hasta,
        out string granularidad)
    {
        var esDiaria = desde.HasValue
            && hasta.HasValue
            && hasta.Value.DayNumber - desde.Value.DayNumber <= 30;
        granularidad = esDiaria ? "diaria" : "mensual";

        var datos = compras
            .GroupBy(compra => esDiaria
                ? DateOnly.FromDateTime(compra.Fecha)
                : new DateOnly(compra.Fecha.Year, compra.Fecha.Month, 1))
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => (
                Etiqueta: esDiaria
                    ? grupo.Key.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)
                    : grupo.Key.ToString("MM/yyyy", CultureInfo.CurrentCulture),
                Valor: grupo.Sum(compra => compra.Total)))
            .ToArray();

        return CrearBarras(datos, esImporte: true);
    }

    private static IReadOnlyList<BarraEstadisticaViewModel> CrearBarrasPorCategoria(
        IReadOnlyCollection<DetalleEstadisticaRegistro> detalles)
    {
        var datos = detalles
            .GroupBy(detalle => detalle.CategoriaId)
            .OrderBy(grupo => grupo.First().NombreCategoria)
            .Select(grupo => (
                Etiqueta: grupo.First().NombreCategoria,
                Valor: (decimal)grupo.Sum(detalle => (long)detalle.Cantidad)))
            .ToArray();

        return CrearBarras(datos, esImporte: false);
    }

    private static IReadOnlyList<BarraEstadisticaViewModel> CrearBarras(
        IReadOnlyCollection<(string Etiqueta, decimal Valor)> datos,
        bool esImporte)
    {
        var maximoAbsoluto = datos
            .Select(dato => Math.Abs(dato.Valor))
            .DefaultIfEmpty(0m)
            .Max();

        return datos
            .Select(dato => new BarraEstadisticaViewModel
            {
                Etiqueta = dato.Etiqueta,
                Valor = dato.Valor,
                EsImporte = esImporte,
                AlturaPixels = maximoAbsoluto == 0
                    ? 0
                    : Math.Max(2, (int)Math.Round((double)(Math.Abs(dato.Valor) / maximoAbsoluto) * 180))
            })
            .ToArray();
    }

    private static string FormatearImporte(decimal importe)
    {
        return importe.ToString("N6", CultureInfo.CurrentCulture);
    }

    internal static decimal PercentilLineal(decimal[] valoresOrdenados, decimal proporcion)
    {
        var posicion = (valoresOrdenados.Length - 1) * proporcion;
        var inferior = (int)decimal.Floor(posicion);
        var fraccion = posicion - inferior;
        var superior = Math.Min(inferior + 1, valoresOrdenados.Length - 1);
        return valoresOrdenados[inferior]
            + fraccion * (valoresOrdenados[superior] - valoresOrdenados[inferior]);
    }

    private static DiagramaCajaViewModel CrearDiagramaCaja(
        decimal[] valoresOrdenados,
        decimal primerCuartil,
        decimal tercerCuartil)
    {
        var rangoIntercuartil = tercerCuartil - primerCuartil;
        var limiteInferior = primerCuartil - 1.5m * rangoIntercuartil;
        var limiteSuperior = tercerCuartil + 1.5m * rangoIntercuartil;
        var bigoteInferior = valoresOrdenados.First(valor => valor >= limiteInferior);
        var bigoteSuperior = valoresOrdenados.Last(valor => valor <= limiteSuperior);
        var minimo = valoresOrdenados[0];
        var maximo = valoresOrdenados[^1];

        int PosicionX(decimal valor)
        {
            if (maximo == minimo)
            {
                return 400;
            }

            var proporcion = (double)((valor - minimo) / (maximo - minimo));
            return 40 + (int)Math.Round(proporcion * 720);
        }

        var atipicos = valoresOrdenados
            .Where(valor => valor < limiteInferior || valor > limiteSuperior)
            .ToArray();

        return new DiagramaCajaViewModel
        {
            Minimo = minimo,
            PrimerCuartil = primerCuartil,
            Mediana = PercentilLineal(valoresOrdenados, 0.5m),
            TercerCuartil = tercerCuartil,
            Maximo = maximo,
            BigoteInferior = bigoteInferior,
            BigoteSuperior = bigoteSuperior,
            ValoresAtipicos = atipicos,
            BigoteInferiorX = PosicionX(bigoteInferior),
            CajaInicioX = PosicionX(primerCuartil),
            MedianaX = PosicionX(PercentilLineal(valoresOrdenados, 0.5m)),
            CajaFinX = PosicionX(tercerCuartil),
            BigoteSuperiorX = PosicionX(bigoteSuperior),
            ValoresAtipicosX = atipicos.Select(PosicionX).ToArray()
        };
    }
}
