using HocicosAJ.Models.Database;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HocicosAJ.Models.ViewModels;

public class CatalogoViewModel
{
    public IReadOnlyList<Producto> Productos { get; set; } = [];

    public IReadOnlyList<SelectListItem> Categorias { get; set; } = [];

    public int? CategoriaIdSeleccionada { get; set; }

    public string? NombreCategoriaSeleccionada { get; set; }

    public string? Busqueda { get; set; }
}
