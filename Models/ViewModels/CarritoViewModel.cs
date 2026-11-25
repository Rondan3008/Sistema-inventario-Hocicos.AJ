using HocicosAJ.Models.Database;

namespace HocicosAJ.Models.ViewModels;

public class CarritoViewModel
{
    public IReadOnlyList<CarritoLineaViewModel> Lineas { get; set; } = [];

    public decimal Total { get; set; }

    public Guid ClaveIdempotencia { get; set; }

    public bool PuedeConfirmar =>
        Lineas.Count > 0 && Lineas.All(linea => linea.Disponible);
}

public class CarritoLineaViewModel
{
    public required int ProductoId { get; init; }

    public Producto? Producto { get; init; }

    public int Cantidad { get; init; }

    public bool PuedeActualizar { get; init; }

    public bool Disponible { get; init; }

    public decimal Subtotal =>
        Disponible && Producto is not null ? Producto.Precio * Cantidad : 0m;
}
