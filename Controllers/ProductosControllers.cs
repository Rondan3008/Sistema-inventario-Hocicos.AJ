using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HocicosAJ.Data;
using HocicosAJ.Models.ViewModels;

namespace HocicosAJ.Controllers
{
    public class ProductosController : Controller
    {
        private readonly HocicosAjContext _context;

        public ProductosController(HocicosAjContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoriaId, string? busqueda)
        {
            busqueda = busqueda?.Trim();
            if (busqueda?.Length > 60)
            {
                return BadRequest("La búsqueda no puede superar los 60 caracteres.");
            }

            var categorias = await _context.Categoria
                .AsNoTracking()
                .Where(categoria => categoria.Activo)
                .OrderBy(categoria => categoria.NombreCategoria)
                .ToListAsync();

            var modelo = new CatalogoViewModel
            {
                CategoriaIdSeleccionada = categoriaId,
                Busqueda = busqueda,
                Categorias = categorias
                    .Select(categoria => new SelectListItem
                    {
                        Value = categoria.Idcategoria.ToString(),
                        Text = categoria.NombreCategoria,
                        Selected = categoria.Idcategoria == categoriaId
                    })
                    .ToList()
            };

            if (categoriaId.HasValue)
            {
                var categoriaSeleccionada = categorias
                    .FirstOrDefault(categoria => categoria.Idcategoria == categoriaId.Value);

                if (categoriaSeleccionada is null)
                {
                    return NotFound();
                }

                modelo.NombreCategoriaSeleccionada = categoriaSeleccionada.NombreCategoria;
            }

            var consulta = _context.Productos
                .AsNoTracking()
                .Include(producto => producto.CategoriaIdcategoriaNavigation)
                .Where(producto =>
                    producto.Activo
                    && producto.CategoriaIdcategoriaNavigation.Activo
                    && producto.Stock > 0);

            if (categoriaId.HasValue)
            {
                consulta = consulta.Where(producto =>
                    producto.CategoriaIdcategoria == categoriaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                consulta = consulta.Where(producto =>
                    producto.Nombre.Contains(busqueda)
                    || (producto.Descripción != null && producto.Descripción.Contains(busqueda)));
            }

            modelo.Productos = await consulta
                .OrderBy(producto => producto.Nombre)
                .ToListAsync();

            return View(modelo);
        }
    }
}