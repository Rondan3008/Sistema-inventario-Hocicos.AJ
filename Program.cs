using System.Globalization;
using System.Security.Claims;
using HocicosAJ.Data;
using HocicosAJ.Models.Database;
using HocicosAJ.Security;
using HocicosAJ.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

if (args.Length == 1 && args[0] == "--generar-hash-admin")
{
    AdminPasswordHashGenerator.Run();
    return;
}

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("HocicosAJ");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta la configuración explícita 'ConnectionStrings:HocicosAJ'. Configure User Secrets, una variable de entorno o un gestor de secretos antes de iniciar la aplicación.");
}

var connectionOptions = new MySqlConnectionStringBuilder(connectionString);
connectionOptions.GuidFormat = MySqlGuidFormat.None;
connectionString = connectionOptions.ConnectionString;
if (builder.Environment.IsDevelopment()
    && !string.Equals(
        connectionOptions.Database,
        "hocicos_aj_test",
        StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "En Development, 'ConnectionStrings:HocicosAJ' debe apuntar exclusivamente a 'hocicos_aj_test'.");
}

var storefrontStoragePath = builder.Configuration["Storefront:StoragePath"];
if (string.IsNullOrWhiteSpace(storefrontStoragePath))
{
    var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (string.IsNullOrWhiteSpace(localApplicationData))
    {
        throw new InvalidOperationException(
            "Configure 'Storefront:StoragePath' con una ruta absoluta para guardar la personalización fuera del contenido publicable.");
    }

    storefrontStoragePath = Path.Combine(localApplicationData, "HocicosAJ", "storefront");
}
else if (!Path.IsPathFullyQualified(storefrontStoragePath))
{
    throw new InvalidOperationException(
        "'Storefront:StoragePath' debe ser una ruta absoluta fuera del directorio de publicación.");
}

var maximumImageSizeBytes = builder.Configuration.GetValue<long?>("Storefront:MaximumImageSizeBytes")
    ?? 5 * 1024 * 1024;
if (maximumImageSizeBytes is < 1024 or > 20 * 1024 * 1024)
{
    throw new InvalidOperationException(
        "'Storefront:MaximumImageSizeBytes' debe estar entre 1024 y 20971520 bytes.");
}

var fullStorefrontStoragePath = Path.GetFullPath(storefrontStoragePath);
var fullContentRoot = Path.TrimEndingDirectorySeparator(
    Path.GetFullPath(builder.Environment.ContentRootPath));
var contentRootPrefix = fullContentRoot + Path.DirectorySeparatorChar;
var pathComparison = OperatingSystem.IsWindows()
    ? StringComparison.OrdinalIgnoreCase
    : StringComparison.Ordinal;
if (string.Equals(fullStorefrontStoragePath, fullContentRoot, pathComparison)
    || fullStorefrontStoragePath.StartsWith(contentRootPrefix, pathComparison))
{
    throw new InvalidOperationException(
        "'Storefront:StoragePath' debe ubicarse fuera del directorio de la aplicación para evitar sobrescrituras durante la publicación.");
}

builder.Services.AddSingleton(new StorefrontStorageOptions(
    fullStorefrontStoragePath)
{
    MaximumImageSizeBytes = maximumImageSizeBytes
});
builder.Services.AddSingleton<StorefrontSettingsStore>();
builder.Services.AddSingleton<StorefrontAssetStore>();
builder.Services.AddScoped<EstadisticasDatosService>();

builder.Services.AddDbContext<HocicosAjContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".HocicosAJ.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/AccesoAdmin";
        options.AccessDeniedPath = "/Home/AccesoDenegado";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async validationContext =>
        {
            var principal = validationContext.Principal;
            var identity = principal?.Identity as ClaimsIdentity;
            var idClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identity is null || !int.TryParse(idClaim, out var userId))
            {
                validationContext.RejectPrincipal();
                await validationContext.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var validationClaim = principal!.FindFirst(UsuarioRoles.ValidationTimestampClaim);
            if (DateTimeOffset.TryParse(
                    validationClaim?.Value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var lastValidation)
                && now >= lastValidation
                && now - lastValidation < TimeSpan.FromMinutes(5))
            {
                return;
            }

            var context = validationContext.HttpContext.RequestServices
                .GetRequiredService<HocicosAjContext>();
            var account = await context.Usuarios
                .AsNoTracking()
                .Where(usuario => usuario.Idusuarios == userId)
                .Select(usuario => usuario.TipoUsuario)
                .SingleOrDefaultAsync();
            var currentRole = UsuarioRoles.Normalizar(account);
            if (currentRole is null)
            {
                validationContext.RejectPrincipal();
                await validationContext.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            foreach (var roleClaim in identity.FindAll(ClaimTypes.Role).ToList())
            {
                identity.RemoveClaim(roleClaim);
            }

            if (validationClaim is not null)
            {
                identity.RemoveClaim(validationClaim);
            }

            identity.AddClaim(new Claim(ClaimTypes.Role, currentRole));
            identity.AddClaim(new Claim(
                UsuarioRoles.ValidationTimestampClaim,
                now.ToString("O", CultureInfo.InvariantCulture)));
            validationContext.ReplacePrincipal(new ClaimsPrincipal(identity));
            validationContext.ShouldRenew = true;
        };
    });

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administradores", policy =>
        policy.RequireAuthenticatedUser().RequireRole("Administrador"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
