using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HocicosAJ.Models.ViewModels;

public class ProductoFormViewModel
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La descripción no puede superar los 200 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "La talla es obligatoria.")]
    [StringLength(45, ErrorMessage = "La talla no puede superar los 45 caracteres.")]
    public string Talla { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "99999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El precio debe estar entre 0,01 y 99.999.999,99.")]
    public decimal Precio { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int CategoriaIdcategoria { get; set; }

    public IFormFile? ImagenArchivo { get; set; }

    public bool QuitarImagen { get; set; }
}
