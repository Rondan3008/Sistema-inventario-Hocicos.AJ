using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HocicosAJ.Models.ViewModels;

public sealed class AdminPersonalizacionViewModel
{
    [Required]
    public string Version { get; set; } = string.Empty;

    [Required(ErrorMessage = "El título principal es obligatorio.")]
    [StringLength(100)]
    public string HeroTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "El subtítulo es obligatorio.")]
    [StringLength(240)]
    public string HeroSubtitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "El texto del botón es obligatorio.")]
    [StringLength(40)]
    public string HeroButtonText { get; set; } = string.Empty;

    [Required]
    public string HeroButtonPath { get; set; } = "/Productos";

    [Required]
    [StringLength(80)]
    public string FeaturedTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string CategoriesTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string InformationTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(240)]
    public string InformationText { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string FooterText { get; set; } = string.Empty;

    public string? LogoImagePath { get; set; }
    public string? HeroImagePath { get; set; }
    public string? DashboardImagePath { get; set; }
    public bool RemoveLogoImage { get; set; }
    public bool RemoveHeroImage { get; set; }
    public bool RemoveDashboardImage { get; set; }
    public IFormFile? LogoFile { get; set; }
    public IFormFile? HeroFile { get; set; }
    public IFormFile? DashboardFile { get; set; }

    public List<SecondaryBannerFormViewModel> Banners { get; set; } = [];
    public List<CategoryImageFormViewModel> Categories { get; set; } = [];
    public List<FeaturedProductOptionViewModel> Products { get; set; } = [];
}

public sealed class SecondaryBannerFormViewModel
{
    public bool IsActive { get; set; }

    [StringLength(80)]
    public string Title { get; set; } = string.Empty;

    [StringLength(180)]
    public string Text { get; set; } = string.Empty;

    [StringLength(40)]
    public string ButtonText { get; set; } = string.Empty;

    public string ButtonPath { get; set; } = "/Productos";
    public string? ImagePath { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool RemoveImage { get; set; }
}

public sealed class CategoryImageFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool RemoveImage { get; set; }
}

public sealed class FeaturedProductOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
