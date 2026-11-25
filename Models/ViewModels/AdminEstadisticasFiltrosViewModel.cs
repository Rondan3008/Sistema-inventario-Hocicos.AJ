using System.ComponentModel.DataAnnotations;

namespace HocicosAJ.Models.ViewModels;

public class AdminEstadisticasFiltrosViewModel
{
    [DataType(DataType.Date)]
    public DateOnly? Desde { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? Hasta { get; set; }

    [Range(0, 100, ErrorMessage = "El percentil debe estar entre 0 y 100.")]
    public int Percentil { get; set; } = 90;

    [Range(0, 1000, ErrorMessage = "k debe estar entre 0 y 1000.")]
    public int K { get; set; }

    [Range(typeof(double), "0", "1000000000000", ParseLimitsInInvariantCulture = true)]
    public double? MediaNormalTeorica { get; set; }

    [Range(typeof(double), "0.000000000001", "1000000000000", ParseLimitsInInvariantCulture = true)]
    public double? DesviacionNormalTeorica { get; set; }

    [Range(typeof(double), "0", "1000000000000", ParseLimitsInInvariantCulture = true)]
    public double? UmbralNormal { get; set; }

    [Range(typeof(double), "0.000000000001", "1000000000000", ParseLimitsInInvariantCulture = true)]
    public double? TasaExponencialTeorica { get; set; }

    [Range(typeof(double), "0", "1000000000000", ParseLimitsInInvariantCulture = true)]
    public double TiempoEsperaHoras { get; set; } = 24;
}
