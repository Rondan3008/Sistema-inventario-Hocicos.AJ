using System;
using System.Collections.Generic;

namespace HocicosAJ.Models.Database;

public partial class Comprobante
{
    public int Idcomprobante { get; set; }

    public string NumeroComprobante { get; set; } = null!;

    public string TipoComprobante { get; set; } = null!;

    public DateTime FechaEmision { get; set; }

    public decimal MontoTotal { get; set; }

    public string MetodoPago { get; set; } = null!;

    public int ComprasIdcompras { get; set; }

    public virtual Compra ComprasIdcomprasNavigation { get; set; } = null!;
}
