using HocicosAJ.Data;
using HocicosAJ.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Policy = "Administradores")]
public class AdminUsuariosController : Controller
{
    private readonly HocicosAjContext _context;

    public AdminUsuariosController(HocicosAjContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminUsuariosIndexViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            modelo.Usuarios = [];
            return View(modelo);
        }

        var busqueda = modelo.Busqueda?.Trim();
        var consulta = _context.Usuarios.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            consulta = consulta.Where(usuario =>
                usuario.Nombre.Contains(busqueda)
                || (usuario.Usuario1 != null && usuario.Usuario1.Contains(busqueda)));
        }

        modelo.Busqueda = busqueda;
        modelo.Usuarios = await consulta
            .OrderBy(usuario => usuario.Nombre)
            .ThenBy(usuario => usuario.Usuario1)
            .Select(usuario => new UsuarioAdminViewModel
            {
                Id = usuario.Idusuarios,
                Nombre = usuario.Nombre,
                Usuario = usuario.Usuario1,
                Rol = usuario.TipoUsuario
            })
            .ToListAsync();

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Where(registro => registro.Idusuarios == id)
            .Select(registro => new UsuarioAdminDetalleViewModel
            {
                Id = registro.Idusuarios,
                Nombre = registro.Nombre,
                Usuario = registro.Usuario1,
                Rol = registro.TipoUsuario,
                Correo = registro.Correo,
                Telefono = registro.Telefono,
                Direccion = registro.Direccion
            })
            .SingleOrDefaultAsync();

        return usuario is null ? NotFound() : View(usuario);
    }
}
