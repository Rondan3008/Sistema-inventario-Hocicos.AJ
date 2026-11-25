using System.Security.Cryptography;
using System.Text.Json;
using HocicosAJ.Models.Storefront;

namespace HocicosAJ.Services;

public sealed class StorefrontStorageOptions(string rootPath)
{
    public string RootPath { get; } = rootPath;
    public long MaximumImageSizeBytes { get; set; } = 5 * 1024 * 1024;
}

public sealed class StorefrontSettingsConflictException : Exception
{
    public StorefrontSettingsConflictException()
        : base("La configuración cambió desde que se abrió el formulario.")
    {
    }
}

public sealed class StorefrontSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly StorefrontStorageOptions _options;
    private readonly SemaphoreSlim _processLock = new(1, 1);
    private readonly string _settingsPath;
    private readonly string _lockPath;

    public StorefrontSettingsStore(StorefrontStorageOptions options)
    {
        _options = options;
        _settingsPath = Path.Combine(options.RootPath, "storefront.json");
        _lockPath = Path.Combine(options.RootPath, ".storefront.lock");
    }

    public async Task<StorefrontSettingsSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.RootPath);
        if (!File.Exists(_settingsPath))
        {
            var defaults = new StorefrontSettings();
            return new StorefrontSettingsSnapshot(defaults, GetVersion(Serialize(defaults)));
        }

        if (new FileInfo(_settingsPath).Length > 1_048_576)
        {
            throw new JsonException("El archivo de personalización supera el tamaño máximo permitido.");
        }

        var bytes = await File.ReadAllBytesAsync(_settingsPath, cancellationToken);
        var settings = JsonSerializer.Deserialize<StorefrontSettings>(bytes, JsonOptions)
            ?? throw new JsonException("El archivo de personalización no contiene una configuración válida.");
        ValidateStoredSettings(settings);
        return new StorefrontSettingsSnapshot(settings, GetVersion(bytes));
    }

    public async Task<string> SaveAsync(
        StorefrontSettings settings,
        string expectedVersion,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.RootPath);
        await _processLock.WaitAsync(cancellationToken);
        try
        {
            await using var fileLock = await AcquireFileLockAsync(cancellationToken);
            var currentBytes = File.Exists(_settingsPath)
                ? await File.ReadAllBytesAsync(_settingsPath, cancellationToken)
                : Serialize(new StorefrontSettings());
            if (!string.Equals(GetVersion(currentBytes), expectedVersion, StringComparison.Ordinal))
            {
                throw new StorefrontSettingsConflictException();
            }

            ValidateStoredSettings(settings);
            var bytes = Serialize(settings);
            var temporaryPath = Path.Combine(
                _options.RootPath,
                $".storefront-{Guid.NewGuid():N}.tmp");
            try
            {
                await using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await stream.WriteAsync(bytes, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temporaryPath, _settingsPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            return GetVersion(bytes);
        }
        finally
        {
            _processLock.Release();
        }
    }

    private async Task<FileStream> AcquireFileLockAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(
                    _lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    1,
                    FileOptions.Asynchronous);
            }
            catch (IOException) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(50, cancellationToken);
            }
            catch (IOException exception)
            {
                throw new IOException("No se pudo bloquear el archivo de personalización para guardar cambios.", exception);
            }
        }
    }

    private static byte[] Serialize(StorefrontSettings settings) =>
        JsonSerializer.SerializeToUtf8Bytes(settings, JsonOptions);

    private static string GetVersion(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static void ValidateStoredSettings(StorefrontSettings settings)
    {
        if (!IsAllowedPath(settings.HeroButtonPath)
            || settings.SecondaryBanners.Any(banner => !IsAllowedPath(banner.ButtonPath)))
        {
            throw new JsonException("La configuración contiene un destino no permitido.");
        }

        if (settings.SecondaryBanners.Count > 3
            || settings.FeaturedProductIds.Count > 12
            || settings.CategoryImagePaths.Keys.Any(id => id <= 0))
        {
            throw new JsonException("La configuración contiene listas fuera de los límites permitidos.");
        }

        var imagePaths = new[]
        {
            settings.LogoImagePath,
            settings.HeroImagePath,
            settings.DashboardImagePath
        }.Concat(settings.CategoryImagePaths.Values)
            .Concat(settings.SecondaryBanners.Select(banner => banner.ImagePath));
        if (imagePaths.Any(path => !IsAllowedImagePath(path)))
        {
            throw new JsonException("La configuración contiene una referencia de imagen no permitida.");
        }
    }

    private static bool IsAllowedPath(string? path) =>
        path is "/Productos" or "/Carrito" or "/Home/Registro";

    private static bool IsAllowedImagePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return true;
        }

        return path.StartsWith("/store-assets/", StringComparison.Ordinal)
            && StorefrontAssetStore.IsGeneratedImageName(path["/store-assets/".Length..]);
    }
}
