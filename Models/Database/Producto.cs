using System;
using System.Collections.Generic;

namespace HocicosAJ.Models.Database;

public partial class Producto
{
    public int Idproductos { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripción { get; set; }

    public string Talla { get; set; } = null!;

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public bool Activo { get; set; }

    public string? Imagen { get; set; }

    public int CategoriaIdcategoria { get; set; }

    public virtual Categorium CategoriaIdcategoriaNavigation { get; set; } = null!;

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();
}
