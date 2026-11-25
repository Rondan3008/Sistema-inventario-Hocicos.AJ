using System;
using System.Collections.Generic;

namespace HocicosAJ.Models.Database;

public partial class Compra
{
    public int Idcompras { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; } = null!;

    public string? ClaveIdempotencia { get; set; }

    public int UsuariosIdusuarios { get; set; }

    public int ClienteIdusuarios { get; set; }

    public virtual Usuario ClienteIdusuariosNavigation { get; set; } = null!;

    public virtual ICollection<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual Usuario UsuariosIdusuariosNavigation { get; set; } = null!;
}
