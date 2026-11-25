using System.ComponentModel.DataAnnotations;

namespace HocicosAJ.Models.ViewModels;

public class RegistroClienteViewModel
{
    [Required(ErrorMessage = "Ingrese su nombre.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese un nombre de usuario.")]
    [StringLength(50, ErrorMessage = "El usuario no puede superar los 50 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use letras sin acento (A-Z), números, punto, guion o guion bajo en el usuario.")]
    public string Usuario { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
    [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "Ingrese una contraseña.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "La contraseña debe tener entre 12 y 128 caracteres.")]
    [DataType(DataType.Password)]
    public string Contraseña { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [Compare(nameof(Contraseña), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarContraseña { get; set; } = string.Empty;
}
