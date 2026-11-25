using HocicosAJ.Models.Database;
using HocicosAJ.Models.Storefront;

namespace HocicosAJ.Models.ViewModels;

public sealed class HomeViewModel
{
    public StorefrontSettings Settings { get; set; } = new();
    public IReadOnlyList<Producto> Productos { get; set; } = [];
    public IReadOnlyList<Categorium> Categorias { get; set; } = [];
}
