namespace HocicosAJ.Models.ViewModels;

public sealed class AdminEstadisticasGraficasViewModel
{
    public IReadOnlyList<DatoGraficaEstadisticaViewModel> UnidadesPorProducto { get; init; } = [];

    public IReadOnlyList<DatoGraficaEstadisticaViewModel> ImportePorProducto { get; init; } = [];

    public IReadOnlyList<DatoGraficaEstadisticaViewModel> ParticipacionPorCategoria { get; init; } = [];

    public IReadOnlyList<DatoGraficaEstadisticaViewModel> HistogramaImportes { get; init; } = [];

    public IReadOnlyList<DatoGraficaEstadisticaViewModel> ComprasPorFecha { get; init; } = [];

    public IReadOnlyList<DatoGraficaEstadisticaViewModel> MetodosPago { get; init; } = [];
}

public sealed class DatoGraficaEstadisticaViewModel
{
    public string Etiqueta { get; init; } = string.Empty;

    public decimal Valor { get; init; }
}
