using System.ComponentModel.DataAnnotations;

namespace HocicosAJ.Models.ViewModels;

public class AdminUsuariosIndexViewModel
{
    [StringLength(100, ErrorMessage = "La búsqueda no puede superar los 100 caracteres.")]
    public string? Busqueda { get; set; }

    public IReadOnlyList<UsuarioAdminViewModel> Usuarios { get; set; } = [];
}

public class UsuarioAdminViewModel
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Usuario { get; set; }

    public string Rol { get; set; } = string.Empty;
}

public class UsuarioAdminDetalleViewModel : UsuarioAdminViewModel
{
    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }
}
