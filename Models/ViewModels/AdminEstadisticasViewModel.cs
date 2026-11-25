namespace HocicosAJ.Models.ViewModels;

public class AdminEstadisticasViewModel
{
    public AdminEstadisticasFiltrosViewModel Filtros { get; set; } = new();

    public int NumeroCompras { get; set; }

    public long UnidadesVendidas { get; set; }

    public bool TieneDetalles { get; set; }

    public decimal? TotalIngresos { get; set; }

    public decimal? ImportePromedio { get; set; }

    public decimal? Media { get; set; }

    public decimal? Mediana { get; set; }

    public decimal? PrimerCuartil { get; set; }

    public decimal? TercerCuartil { get; set; }

    public decimal? PercentilSeleccionado { get; set; }

    public IReadOnlyList<decimal> Modas { get; set; } = [];

    public decimal? Rango { get; set; }

    public double? VarianzaPoblacional { get; set; }

    public double? VarianzaMuestral { get; set; }

    public double? DesviacionEstandarPoblacional { get; set; }

    public double? DesviacionEstandarMuestral { get; set; }

    public double? CoeficienteVariacionPorcentual { get; set; }

    public double? AsimetriaPoblacional { get; set; }

    public double? CurtosisExcesoPoblacional { get; set; }

    public DiagramaCajaViewModel? DiagramaCaja { get; set; }

    public string? ProductoMasVendido { get; set; }

    public long? UnidadesProductoMasVendido { get; set; }

    public string? CategoriaMasVendida { get; set; }

    public long? UnidadesCategoriaMasVendida { get; set; }

    public string GranularidadVentas { get; set; } = "mensual";

    public IReadOnlyList<BarraEstadisticaViewModel> VentasPorPeriodo { get; set; } = [];

    public IReadOnlyList<BarraEstadisticaViewModel> VentasPorCategoria { get; set; } = [];

    public IReadOnlyList<FrecuenciaImporteViewModel> FrecuenciasImportes { get; set; } = [];

    public AnalisisProbabilisticoViewModel AnalisisProbabilistico { get; set; } = new();
}

public class BarraEstadisticaViewModel
{
    public string Etiqueta { get; init; } = string.Empty;

    public decimal Valor { get; init; }

    public bool EsImporte { get; init; }

    public int AlturaPixels { get; init; }

    public string ValorFormateado =>
        EsImporte ? Valor.ToString("N2") : Valor.ToString("N0");
}

public class FrecuenciaImporteViewModel
{
    public string Intervalo { get; init; } = string.Empty;

    public int Frecuencia { get; init; }

    public int FrecuenciaAcumulada { get; init; }

    public double FrecuenciaRelativaPorcentual { get; init; }

    public int AlturaPixels { get; init; }
}
