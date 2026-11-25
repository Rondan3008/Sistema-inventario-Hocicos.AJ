using System.ComponentModel.DataAnnotations;

namespace HocicosAJ.Models.ViewModels;

public class AdminComprasFiltrosViewModel
{
    [DataType(DataType.Date)]
    public DateOnly? Desde { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? Hasta { get; set; }

    [StringLength(100, ErrorMessage = "La búsqueda de cliente no puede superar 100 caracteres.")]
    public string? Cliente { get; set; }
}

public class AdminComprasIndexViewModel
{
    public AdminComprasFiltrosViewModel Filtros { get; set; } = new();

    public IReadOnlyList<AdminCompraResumenViewModel> Compras { get; set; } = [];
}

public class AdminCompraResumenViewModel
{
    public int Id { get; init; }

    public DateTime Fecha { get; init; }

    public string Estado { get; init; } = string.Empty;

    public decimal Total { get; init; }

    public string ClienteNombre { get; init; } = string.Empty;

    public string? ClienteUsuario { get; init; }
}

public class AdminCompraDetalleViewModel
{
    public int Id { get; init; }

    public DateTime Fecha { get; init; }

    public string Estado { get; init; } = string.Empty;

    public decimal Total { get; init; }

    public string ClienteNombre { get; init; } = string.Empty;

    public string? ClienteUsuario { get; init; }

    public IReadOnlyList<AdminCompraLineaViewModel> Detalles { get; init; } = [];
}

public class AdminCompraLineaViewModel
{
    public string Producto { get; init; } = string.Empty;

    public string Talla { get; init; } = string.Empty;

    public int Cantidad { get; init; }

    public decimal PrecioUnitario { get; init; }

    public decimal Subtotal { get; init; }
}
