using System;
using System.Collections.Generic;

namespace HocicosAJ.Models.Database;

public partial class DetalleCompra
{
    public int Iddetalle { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public int ProductosIdproductos { get; set; }

    public int ComprasIdcompras { get; set; }

    public virtual Compra ComprasIdcomprasNavigation { get; set; } = null!;

    public virtual Producto ProductosIdproductosNavigation { get; set; } = null!;
}
