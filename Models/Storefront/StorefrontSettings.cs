namespace HocicosAJ.Models.Storefront;

public sealed class StorefrontSettings
{
    public string LogoImagePath { get; set; } = string.Empty;
    public string HeroTitle { get; set; } = "Comodidad para cada paseo. Estilo para cada aventura.";
    public string HeroSubtitle { get; set; } = "Encuentra prendas para tu compañero, consulta tallas y disponibilidad.";
    public string HeroButtonText { get; set; } = "Explorar productos";
    public string HeroButtonPath { get; set; } = "/Productos";
    public string HeroImagePath { get; set; } = string.Empty;
    public string FeaturedTitle { get; set; } = "Productos para explorar";
    public string CategoriesTitle { get; set; } = "Explora por categoría";
    public string InformationTitle { get; set; } = "Compra con información clara";
    public string InformationText { get; set; } = "Consulta la talla, el precio y la disponibilidad de cada producto antes de comprar.";
    public string FooterText { get; set; } = "Ropa para perros";
    public string DashboardImagePath { get; set; } = string.Empty;
    public List<int> FeaturedProductIds { get; set; } = [];
    public Dictionary<int, string> CategoryImagePaths { get; set; } = [];
    public List<StorefrontBanner> SecondaryBanners { get; set; } =
    [
        new(),
        new(),
        new()
    ];
}

public sealed class StorefrontBanner
{
    public bool IsActive { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string ButtonPath { get; set; } = "/Productos";
    public string ImagePath { get; set; } = string.Empty;
}

public sealed record StorefrontSettingsSnapshot(StorefrontSettings Settings, string Version);
