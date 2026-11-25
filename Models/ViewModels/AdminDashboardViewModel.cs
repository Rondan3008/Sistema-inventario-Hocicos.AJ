namespace HocicosAJ.Models.ViewModels;

public sealed class AdminDashboardViewModel
{
    public int ProductosActivos { get; set; }
    public int ProductosSinStock { get; set; }
    public int CategoriasActivas { get; set; }
    public int UsuariosRegistrados { get; set; }
    public int ComprasUltimos30Dias { get; set; }
    public decimal ImporteRegistradoUltimos30Dias { get; set; }
    public IReadOnlyList<ProductoStockViewModel> InventarioBajo { get; set; } = [];
    public AdminEstadisticasViewModel Estadisticas { get; set; } = new();
}

public sealed class ProductoStockViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Stock { get; set; }
}
