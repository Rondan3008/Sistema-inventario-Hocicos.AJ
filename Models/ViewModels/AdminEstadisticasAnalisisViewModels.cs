namespace HocicosAJ.Models.ViewModels;

public class DiagramaCajaViewModel
{
    public decimal Minimo { get; init; }

    public decimal PrimerCuartil { get; init; }

    public decimal Mediana { get; init; }

    public decimal TercerCuartil { get; init; }

    public decimal Maximo { get; init; }

    public decimal BigoteInferior { get; init; }

    public decimal BigoteSuperior { get; init; }

    public IReadOnlyList<decimal> ValoresAtipicos { get; init; } = [];

    public int BigoteInferiorX { get; init; }

    public int CajaInicioX { get; init; }

    public int MedianaX { get; init; }

    public int CajaFinX { get; init; }

    public int BigoteSuperiorX { get; init; }

    public IReadOnlyList<int> ValoresAtipicosX { get; init; } = [];
}

public class AnalisisProbabilisticoViewModel
{
    public ProbabilidadEmpiricaViewModel Probabilidades { get; set; } = new();

    public PoissonViewModel Poisson { get; set; } = new();

    public DistribucionesContinuasViewModel DistribucionesContinuas { get; set; } = new();
}

public class ProbabilidadEmpiricaViewModel
{
    public int EspacioMuestral { get; set; }

    public int CantidadEventoA { get; set; }

    public int? CantidadEventoB { get; set; }

    public int? CantidadInterseccion { get; set; }

    public decimal? UmbralImporte { get; set; }

    public string DescripcionEventoA { get; set; } =
        "Compra con importe igual o superior a la mediana del período.";

    public string DescripcionEventoB { get; set; } =
        "Compra que incluye una unidad o más de una categoría con máximo de unidades vendidas.";

    public string? CategoriasEventoB { get; set; }

    public double? ProbabilidadA { get; set; }

    public double? ProbabilidadB { get; set; }

    public double? ProbabilidadInterseccion { get; set; }

    public double? ProbabilidadCondicional { get; set; }

    public IReadOnlyList<BarraProbabilidadViewModel> Grafico { get; set; } = [];
}

public class PoissonViewModel
{
    public bool Disponible { get; set; }

    public long PeriodosObservados { get; set; }

    public string Periodo { get; set; } = "mes";

    public int ComprasObservadas { get; set; }

    public int K { get; set; }

    public double? Lambda { get; set; }

    public double? ValorEsperado { get; set; }

    public double? VarianzaTeorica { get; set; }

    public double? VarianzaEmpirica { get; set; }

    public double? ProbabilidadExacta { get; set; }

    public double? ProbabilidadMenorIgual { get; set; }

    public double? ProbabilidadMayorIgual { get; set; }

    public bool PeriodosDelGraficoRecortados { get; set; }

    public string? Advertencia { get; set; }

    public IReadOnlyList<BarraProbabilidadViewModel> Grafico { get; set; } = [];
}

public class DistribucionesContinuasViewModel
{
    public NormalTeoricaViewModel Normal { get; set; } = new();

    public ExponencialViewModel Exponencial { get; set; } = new();
}

public class NormalTeoricaViewModel
{
    public bool Calculada { get; set; }

    public double? Media { get; set; }

    public double? DesviacionEstandar { get; set; }

    public double? Umbral { get; set; }

    public double? Z { get; set; }

    public double? ProbabilidadMenorIgual { get; set; }

    public double? ProbabilidadMayorIgual { get; set; }

    public double? PercentilSeleccionado { get; set; }

    public double? LimiteInferior95 { get; set; }

    public double? LimiteSuperior95 { get; set; }

    public string? Advertencia { get; set; }

    public CurvaDistribucionViewModel? Grafico { get; set; }
}

public class ExponencialViewModel
{
    public bool Calculada { get; set; }

    public bool EsAjusteEmpirico { get; set; }

    public double? TasaPorHora { get; set; }

    public double? MediaEsperaHoras { get; set; }

    public double TiempoConsultaHoras { get; set; }

    public double? ProbabilidadHastaTiempo { get; set; }

    public double? PercentilSeleccionadoHoras { get; set; }

    public double? LimiteInferior95Horas { get; set; }

    public double? LimiteSuperior95Horas { get; set; }

    public int IntervalosEntreCompras { get; set; }

    public string? FuenteParametro { get; set; }

    public string? Advertencia { get; set; }

    public CurvaDistribucionViewModel? Grafico { get; set; }
}

public class CurvaDistribucionViewModel
{
    public string EtiquetaEjeX { get; init; } = string.Empty;

    public string EtiquetaMinimo { get; init; } = string.Empty;

    public string EtiquetaMaximo { get; init; } = string.Empty;

    public IReadOnlyList<PuntoCurvaViewModel> Puntos { get; init; } = [];
}

public class PuntoCurvaViewModel
{
    public int X { get; init; }

    public int Y { get; init; }
}

public class BarraProbabilidadViewModel
{
    public string Etiqueta { get; init; } = string.Empty;

    public double Probabilidad { get; init; }

    public int AlturaPixels { get; init; }

    public string Porcentaje => $"{Probabilidad * 100:N2}%";
}
