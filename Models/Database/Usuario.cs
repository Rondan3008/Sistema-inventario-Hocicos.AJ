using System;
using System.Collections.Generic;

namespace HocicosAJ.Models.Database;

public partial class Usuario
{
    public int Idusuarios { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Usuario1 { get; set; }

    public string? Contraseña { get; set; }

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public string TipoUsuario { get; set; } = null!;

    public string? RolTrabajo { get; set; }

    public virtual ICollection<Compra> CompraClienteIdusuariosNavigations { get; set; } = new List<Compra>();

    public virtual ICollection<Compra> CompraUsuariosIdusuariosNavigations { get; set; } = new List<Compra>();
}
