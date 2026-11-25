using System.ComponentModel.DataAnnotations;

namespace HocicosAJ.Models.ViewModels;

public class CategoriaFormViewModel
{
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    [StringLength(45, ErrorMessage = "El nombre no puede superar los 45 caracteres.")]
    public string NombreCategoria { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La descripción no puede superar los 200 caracteres.")]
    public string? Descripcion { get; set; }
}
