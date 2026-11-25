using HocicosAJ.Data;
using HocicosAJ.Models.Storefront;
using HocicosAJ.Models.ViewModels;
using HocicosAJ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HocicosAJ.Controllers;

[Authorize(Policy = "Administradores")]
public sealed class AdminPersonalizacionController(
    HocicosAjContext context,
    StorefrontSettingsStore settingsStore,
    StorefrontAssetStore assetStore,
    StorefrontStorageOptions storageOptions,
    ILogger<AdminPersonalizacionController> logger) : Controller
{
    private static readonly string[] AllowedButtonPaths =
    [
        "/Productos",
        "/Carrito",
        "/Home/Registro"
    ];

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var snapshot = await settingsStore.GetAsync(cancellationToken);
        return View(await CrearModeloAsync(snapshot.Settings, snapshot.Version, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(40_000_000)]
    public async Task<IActionResult> Index(
        AdminPersonalizacionViewModel modelo,
        CancellationToken cancellationToken)
    {
        modelo.Banners ??= [];
        modelo.Categories ??= [];
        modelo.Products ??= [];

        if (!AllowedButtonPaths.Contains(modelo.HeroButtonPath, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(modelo.HeroButtonPath), "Seleccione un destino local permitido.");
        }

        if (modelo.Banners.Count != 3)
        {
            ModelState.AddModelError(nameof(modelo.Banners), "Se requieren tres espacios de banners secundarios.");
        }

        foreach (var (banner, index) in modelo.Banners.Select((banner, index) => (banner, index)))
        {
            if (!AllowedButtonPaths.Contains(banner.ButtonPath, StringComparer.Ordinal))
            {
                ModelState.AddModelError(
                    $"Banners[{index}].ButtonPath",
                    "Seleccione un destino local permitido.");
            }

            if (banner.IsActive
                && (string.IsNullOrWhiteSpace(banner.Title)
                    || string.IsNullOrWhiteSpace(banner.Text)))
            {
                ModelState.AddModelError(
                    $"Banners[{index}].Title",
                    "Los banners activos requieren título y texto.");
            }
        }

        var categoriaIdsActuales = await context.Categoria
            .AsNoTracking()
            .Select(categoria => categoria.Idcategoria)
            .ToArrayAsync(cancellationToken);
        if (modelo.Categories.Count != categoriaIdsActuales.Length
            || modelo.Categories.Select(categoria => categoria.Id).Distinct().Count()
                != modelo.Categories.Count
            || modelo.Categories.Any(categoria => !categoriaIdsActuales.Contains(categoria.Id)))
        {
            ModelState.AddModelError(
                nameof(modelo.Categories),
                "La lista de categorías cambió. Vuelva a cargar esta página.");
        }

        var productosSeleccionados = modelo.Products
            .Where(producto => producto.Selected)
            .Select(producto => producto.Id)
            .Distinct()
            .ToList();
        if (productosSeleccionados.Count > 12)
        {
            ModelState.AddModelError(nameof(modelo.Products), "Seleccione como máximo 12 productos destacados.");
        }
        else if (productosSeleccionados.Count > 0)
        {
            var productosValidos = await context.Productos
                .AsNoTracking()
                .Where(producto =>
                    productosSeleccionados.Contains(producto.Idproductos)
                    && producto.Activo
                    && producto.Stock > 0
                    && producto.CategoriaIdcategoriaNavigation.Activo)
                .Select(producto => producto.Idproductos)
                .ToArrayAsync(cancellationToken);
            if (productosValidos.Length != productosSeleccionados.Count)
            {
                ModelState.AddModelError(
                    nameof(modelo.Products),
                    "Los productos destacados deben existir, estar activos y tener disponibilidad.");
            }
        }

        if (!ModelState.IsValid)
        {
            var actual = await settingsStore.GetAsync(cancellationToken);
            await CompletarOpcionesAsync(modelo, actual.Settings, cancellationToken);
            return View(modelo);
        }

        var current = await settingsStore.GetAsync(cancellationToken);
        var settings = current.Settings;
        settings.HeroTitle = modelo.HeroTitle.Trim();
        settings.HeroSubtitle = modelo.HeroSubtitle.Trim();
        settings.HeroButtonText = modelo.HeroButtonText.Trim();
        settings.HeroButtonPath = modelo.HeroButtonPath;
        settings.FeaturedTitle = modelo.FeaturedTitle.Trim();
        settings.CategoriesTitle = modelo.CategoriesTitle.Trim();
        settings.InformationTitle = modelo.InformationTitle.Trim();
        settings.InformationText = modelo.InformationText.Trim();
        settings.FooterText = modelo.FooterText.Trim();
        settings.FeaturedProductIds = productosSeleccionados.ToList();

        if (modelo.RemoveLogoImage)
        {
            settings.LogoImagePath = string.Empty;
        }

        if (modelo.RemoveHeroImage)
        {
            settings.HeroImagePath = string.Empty;
        }

        if (modelo.RemoveDashboardImage)
        {
            settings.DashboardImagePath = string.Empty;
        }

        try
        {
            settings.LogoImagePath = await GuardarImagenSiExisteAsync(
                modelo.LogoFile,
                settings.LogoImagePath,
                cancellationToken);
            settings.HeroImagePath = await GuardarImagenSiExisteAsync(
                modelo.HeroFile,
                settings.HeroImagePath,
                cancellationToken);
            settings.DashboardImagePath = await GuardarImagenSiExisteAsync(
                modelo.DashboardFile,
                settings.DashboardImagePath,
                cancellationToken);

            settings.SecondaryBanners = [];
            foreach (var (banner, index) in modelo.Banners.Select((banner, index) => (banner, index)))
            {
                var previous = current.Settings.SecondaryBanners.ElementAtOrDefault(index) ?? new StorefrontBanner();
                var imagePath = banner.RemoveImage ? string.Empty : previous.ImagePath;
                imagePath = await GuardarImagenSiExisteAsync(banner.ImageFile, imagePath, cancellationToken);
                settings.SecondaryBanners.Add(new StorefrontBanner
                {
                    IsActive = banner.IsActive,
                    Title = banner.Title.Trim(),
                    Text = banner.Text.Trim(),
                    ButtonText = banner.ButtonText.Trim(),
                    ButtonPath = banner.ButtonPath,
                    ImagePath = imagePath
                });
            }

            settings.CategoryImagePaths = [];
            foreach (var category in modelo.Categories)
            {
                var imagePath = category.RemoveImage
                    ? string.Empty
                    : current.Settings.CategoryImagePaths.GetValueOrDefault(category.Id, string.Empty);
                imagePath = await GuardarImagenSiExisteAsync(category.ImageFile, imagePath, cancellationToken);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    settings.CategoryImagePaths[category.Id] = imagePath;
                }
            }

            await settingsStore.SaveAsync(settings, modelo.Version, cancellationToken);
        }
        catch (InvalidStorefrontImageException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await CompletarOpcionesAsync(modelo, current.Settings, cancellationToken);
            return View(modelo);
        }
        catch (StorefrontSettingsConflictException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Otro administrador guardó cambios. Recargue la página antes de volver a guardar.");
            await CompletarOpcionesAsync(modelo, (await settingsStore.GetAsync(cancellationToken)).Settings, cancellationToken);
            return View(modelo);
        }
        catch (IOException exception)
        {
            logger.LogError(exception, "No se pudo guardar la personalización de la tienda.");
            ModelState.AddModelError(string.Empty, "No fue posible guardar los cambios. Revise el almacenamiento configurado.");
            await CompletarOpcionesAsync(modelo, current.Settings, cancellationToken);
            return View(modelo);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogError(exception, "No hay permisos para guardar la personalización de la tienda.");
            ModelState.AddModelError(string.Empty, "No hay permisos de escritura en el almacenamiento configurado.");
            await CompletarOpcionesAsync(modelo, current.Settings, cancellationToken);
            return View(modelo);
        }

        TempData["Success"] = "La personalización se guardó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GuardarImagenSiExisteAsync(
        IFormFile? file,
        string currentPath,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return currentPath;
        }

        return await assetStore.SaveImageAsync(file, cancellationToken);
    }

    private async Task<AdminPersonalizacionViewModel> CrearModeloAsync(
        StorefrontSettings settings,
        string version,
        CancellationToken cancellationToken)
    {
        var model = new AdminPersonalizacionViewModel
        {
            Version = version,
            HeroTitle = settings.HeroTitle,
            HeroSubtitle = settings.HeroSubtitle,
            HeroButtonText = settings.HeroButtonText,
            HeroButtonPath = settings.HeroButtonPath,
            FeaturedTitle = settings.FeaturedTitle,
            CategoriesTitle = settings.CategoriesTitle,
            InformationTitle = settings.InformationTitle,
            InformationText = settings.InformationText,
            FooterText = settings.FooterText,
            LogoImagePath = settings.LogoImagePath,
            HeroImagePath = settings.HeroImagePath,
            DashboardImagePath = settings.DashboardImagePath,
            Banners = settings.SecondaryBanners
                .Take(3)
                .Select(banner => new SecondaryBannerFormViewModel
                {
                    IsActive = banner.IsActive,
                    Title = banner.Title,
                    Text = banner.Text,
                    ButtonText = banner.ButtonText,
                    ButtonPath = banner.ButtonPath,
                    ImagePath = banner.ImagePath
                })
                .ToList()
        };
        while (model.Banners.Count < 3)
        {
            model.Banners.Add(new SecondaryBannerFormViewModel());
        }

        await CompletarOpcionesAsync(model, settings, cancellationToken);
        return model;
    }

    private async Task CompletarOpcionesAsync(
        AdminPersonalizacionViewModel model,
        StorefrontSettings settings,
        CancellationToken cancellationToken)
    {
        ViewData["MaximumImageSizeMb"] = (int)Math.Ceiling(
            storageOptions.MaximumImageSizeBytes / 1024d / 1024d);
        model.LogoImagePath = settings.LogoImagePath;
        model.HeroImagePath = settings.HeroImagePath;
        model.DashboardImagePath = settings.DashboardImagePath;
        for (var index = 0; index < model.Banners.Count; index++)
        {
            model.Banners[index].ImagePath = settings.SecondaryBanners
                .ElementAtOrDefault(index)
                ?.ImagePath;
        }

        var categories = await context.Categoria
            .AsNoTracking()
            .OrderBy(categoria => categoria.NombreCategoria)
            .Select(categoria => new { categoria.Idcategoria, categoria.NombreCategoria })
            .ToListAsync(cancellationToken);
        var postedCategories = model.Categories
            .GroupBy(category => category.Id)
            .ToDictionary(group => group.Key, group => group.First());
        model.Categories = categories
            .Select(category =>
            {
                var hasPosted = postedCategories.TryGetValue(category.Idcategoria, out var posted);
                return new CategoryImageFormViewModel
                {
                    Id = category.Idcategoria,
                    Name = category.NombreCategoria,
                    ImagePath = settings.CategoryImagePaths.GetValueOrDefault(category.Idcategoria, string.Empty),
                    ImageFile = hasPosted ? posted!.ImageFile : null,
                    RemoveImage = hasPosted && posted!.RemoveImage
                };
            })
            .ToList();

        var postedProducts = model.Products
            .Where(product => product.Selected)
            .Select(product => product.Id)
            .ToHashSet();
        model.Products = await context.Productos
            .AsNoTracking()
            .Where(producto =>
                producto.Activo
                && producto.Stock > 0
                && producto.CategoriaIdcategoriaNavigation.Activo)
            .OrderBy(producto => producto.Nombre)
            .Select(producto => new FeaturedProductOptionViewModel
            {
                Id = producto.Idproductos,
                Name = producto.Nombre,
                Selected = postedProducts.Count > 0
                    ? postedProducts.Contains(producto.Idproductos)
                    : settings.FeaturedProductIds.Contains(producto.Idproductos)
            })
            .ToListAsync(cancellationToken);
    }
}
