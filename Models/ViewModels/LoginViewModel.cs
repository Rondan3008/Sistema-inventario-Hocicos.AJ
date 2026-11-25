using System.ComponentModel.DataAnnotations;

namespace HocicosAJ.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su usuario.")]
    [StringLength(50, ErrorMessage = "El usuario no puede superar los 50 caracteres.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    public string Contraseña { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
