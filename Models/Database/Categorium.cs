using System;
using System.Collections.Generic;

namespace HocicosAJ.Models.Database;

public partial class Categorium
{
    public int Idcategoria { get; set; }

    public string NombreCategoria { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
