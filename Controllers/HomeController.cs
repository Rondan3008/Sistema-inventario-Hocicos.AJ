using System.Diagnostics;
using System.Security.Claims;
using HocicosAJ.Models;
using HocicosAJ.Data;
using HocicosAJ.Models.Database;
using HocicosAJ.Models.ViewModels;
using HocicosAJ.Models.Storefront;
using HocicosAJ.Security;
using HocicosAJ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

public class HomeController : Controller
{
    private readonly HocicosAjContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly ILogger<HomeController> _logger;
    private readonly StorefrontSettingsStore _storefrontSettingsStore;

    public HomeController(
        HocicosAjContext context,
        IPasswordHasher<Usuario> passwordHasher,
        ILogger<HomeController> logger,
        StorefrontSettingsStore storefrontSettingsStore)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _storefrontSettingsStore = storefrontSettingsStore;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await _storefrontSettingsStore.GetAsync(cancellationToken);
        var categorias = await _context.Categoria
            .AsNoTracking()
            .Where(categoria => categoria.Activo)
            .OrderBy(categoria => categoria.NombreCategoria)
            .ToListAsync(cancellationToken);
        var consulta = _context.Productos
            .AsNoTracking()
            .Include(producto => producto.CategoriaIdcategoriaNavigation)
            .Where(producto =>
                producto.Activo
                && producto.Stock > 0
                && producto.CategoriaIdcategoriaNavigation.Activo);

        var destacados = settings.Settings.FeaturedProductIds
            .Where(id => id > 0)
            .Distinct()
            .Take(12)
            .ToList();
        var productos = destacados.Count > 0
            ? await consulta
                .Where(producto => destacados.Contains(producto.Idproductos))
                .ToListAsync(cancellationToken)
            : [];
        if (destacados.Count > 0)
        {
            if (productos.Count > 0)
            {
                var ordenDestacados = destacados
                    .Select((id, index) => (id, index))
                    .ToDictionary(item => item.id, item => item.index);
                productos = productos
                    .OrderBy(producto => ordenDestacados[producto.Idproductos])
                    .ToList();
            }
        }

        if (productos.Count == 0)
        {
            productos = await consulta
                .OrderBy(producto => producto.Nombre)
                .Take(6)
                .ToListAsync(cancellationToken);
        }

        return View(new HomeViewModel
        {
            Settings = settings.Settings,
            Productos = productos,
            Categorias = categorias
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult AccesoAdmin()
    {
        return View(new LoginViewModel
        {
            ReturnUrl = Request.Query["ReturnUrl"].ToString()
        });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Registro()
    {
        return View(new RegistroClienteViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroClienteViewModel modelo)
    {
        var nombre = modelo.Nombre?.Trim() ?? string.Empty;
        var nombreUsuario = modelo.Usuario?.Trim() ?? string.Empty;
        if (nombre.Length == 0)
        {
            ModelState.AddModelError(nameof(modelo.Nombre), "Ingrese su nombre.");
        }

        if (nombreUsuario.Length == 0)
        {
            ModelState.AddModelError(nameof(modelo.Usuario), "Ingrese un nombre de usuario.");
        }
        else if (await _context.Usuarios.AnyAsync(usuario => usuario.Usuario1 == nombreUsuario))
        {
            ModelState.AddModelError(
                nameof(modelo.Usuario),
                "Ese nombre de usuario ya está registrado.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Usuario1 = nombreUsuario,
            Correo = string.IsNullOrWhiteSpace(modelo.Correo) ? null : modelo.Correo.Trim(),
            TipoUsuario = UsuarioRoles.Cliente
        };
        usuario.Contraseña = _passwordHasher.HashPassword(usuario, modelo.Contraseña);

        if (usuario.Contraseña.Length > 250)
        {
            _logger.LogError("El hash generado supera la capacidad del campo contraseña.");
            ModelState.AddModelError(
                string.Empty,
                "No fue posible registrar la cuenta. Inténtelo nuevamente.");
            return View(modelo);
        }

        try
        {
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "No se pudo registrar una cuenta de cliente.");
            ModelState.AddModelError(
                nameof(modelo.Usuario),
                "No fue posible registrar la cuenta. Compruebe los datos e inténtelo nuevamente.");
            return View(modelo);
        }

        TempData["Success"] = "La cuenta se creó correctamente. Ya puede iniciar sesión.";
        return RedirectToAction(nameof(AccesoAdmin));
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AccesoAdmin(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var nombreUsuario = modelo.Usuario.Trim();
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(registro => registro.Usuario1 == nombreUsuario);

        if (usuario is null || string.IsNullOrWhiteSpace(usuario.Contraseña))
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(modelo);
        }

        PasswordVerificationResult resultado;
        try
        {
            resultado = _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.Contraseña,
                modelo.Contraseña);
        }
        catch (FormatException exception)
        {
            _logger.LogWarning(exception, "Se encontró un hash de contraseña inválido para el usuario {UsuarioId}.", usuario.Idusuarios);
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(modelo);
        }

        var rol = UsuarioRoles.Normalizar(usuario.TipoUsuario);
        if (resultado == PasswordVerificationResult.Failed || rol is null)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(modelo);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var previousUserId)
            || previousUserId != usuario.Idusuarios)
        {
            CarritoSesion.Guardar(HttpContext.Session, []);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Idusuarios.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, rol),
            new Claim(UsuarioRoles.ValidationTimestampClaim, DateTimeOffset.UtcNow.ToString("O"))
        };
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        if (!string.IsNullOrWhiteSpace(modelo.ReturnUrl) && Url.IsLocalUrl(modelo.ReturnUrl))
        {
            return LocalRedirect(modelo.ReturnUrl);
        }

        return rol == UsuarioRoles.Administrador
            ? RedirectToAction("Index", "AdminProductos")
            : RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        CarritoSesion.Guardar(HttpContext.Session, []);
        return RedirectToAction(nameof(AccesoAdmin));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
