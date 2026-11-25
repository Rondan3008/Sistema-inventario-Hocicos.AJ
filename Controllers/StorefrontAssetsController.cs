using HocicosAJ.Services;
using Microsoft.AspNetCore.Mvc;

namespace HocicosAJ.Controllers;

[Route("store-assets")]
public sealed class StorefrontAssetsController(StorefrontAssetStore assetStore) : Controller
{
    [HttpGet("{fileName}")]
    public async Task<IActionResult> Get(string fileName, CancellationToken cancellationToken)
    {
        var stream = await assetStore.OpenReadAsync(fileName, cancellationToken);
        if (stream is null)
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        var contentType = Path.GetExtension(fileName) switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return File(stream, contentType);
    }
}
