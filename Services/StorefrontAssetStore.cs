using System.Buffers.Binary;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace HocicosAJ.Services;

public sealed class InvalidStorefrontImageException(string message) : Exception(message);

public sealed class StorefrontAssetStore
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private readonly StorefrontStorageOptions _options;
    private readonly string _assetDirectory;

    public StorefrontAssetStore(StorefrontStorageOptions options)
    {
        _options = options;
        _assetDirectory = Path.Combine(options.RootPath, "uploads");
    }

    public async Task<string> SaveImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > _options.MaximumImageSizeBytes)
        {
            var maximumMegabytes = _options.MaximumImageSizeBytes / 1024d / 1024d;
            throw new InvalidStorefrontImageException(
                $"La imagen debe pesar entre 1 byte y {maximumMegabytes:0.##} MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        {
            throw new InvalidStorefrontImageException("Use una imagen JPG, PNG o WebP.");
        }

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        await input.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (bytes.LongLength > _options.MaximumImageSizeBytes)
        {
            throw new InvalidStorefrontImageException("La imagen supera el tamaño máximo permitido.");
        }

        var actualExtension = GetValidatedExtension(bytes);
        if (extension is ".jpg" or ".jpeg")
        {
            if (actualExtension != ".jpg")
            {
                throw new InvalidStorefrontImageException("La extensión del archivo no coincide con su contenido.");
            }
        }
        else if (!string.Equals(extension, actualExtension, StringComparison.Ordinal))
        {
            throw new InvalidStorefrontImageException("La extensión del archivo no coincide con su contenido.");
        }

        Directory.CreateDirectory(_assetDirectory);
        var fileName = $"{Guid.NewGuid():N}{actualExtension}";
        var destination = Path.Combine(_assetDirectory, fileName);
        var temporaryPath = Path.Combine(_assetDirectory, $".{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await output.WriteAsync(bytes, cancellationToken);
                await output.FlushAsync(cancellationToken);
                output.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destination);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return $"/store-assets/{fileName}";
    }

    public Task<FileStream?> OpenReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsGeneratedImageName(fileName))
        {
            return Task.FromResult<FileStream?>(null);
        }

        var path = Path.Combine(_assetDirectory, fileName);
        if (!File.Exists(path))
        {
            return Task.FromResult<FileStream?>(null);
        }

        return Task.FromResult<FileStream?>(new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan));
    }

    public static bool IsGeneratedImageName(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || Path.GetFileName(fileName) != fileName)
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        return name.Length == 32
            && name.All(Uri.IsHexDigit)
            && extension is ".jpg" or ".png" or ".webp";
    }

    private static string GetValidatedExtension(byte[] bytes)
    {
        if (IsPng(bytes))
        {
            return ".png";
        }

        if (IsJpeg(bytes))
        {
            return ".jpg";
        }

        if (IsWebp(bytes))
        {
            return ".webp";
        }

        throw new InvalidStorefrontImageException(
            "El contenido no coincide con una imagen JPG, PNG o WebP válida.");
    }

    private static bool IsPng(byte[] bytes)
    {
        if (bytes.Length < 45 || !bytes.AsSpan(0, 8).SequenceEqual(PngSignature))
        {
            return false;
        }

        var offset = 8;
        var hasHeader = false;
        var hasImageData = false;
        var hasEnd = false;
        uint width = 0;
        uint height = 0;
        while (offset <= bytes.Length - 12)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length > int.MaxValue || offset + 12L + length > bytes.Length)
            {
                return false;
            }

            var chunkType = Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var dataOffset = offset + 8;
            var dataLength = (int)length;
            var crcOffset = dataOffset + dataLength;
            if (ReadPngCrc(bytes.AsSpan(offset + 4, dataLength + 4))
                != BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(crcOffset, 4)))
            {
                return false;
            }

            if (!hasHeader)
            {
                if (chunkType != "IHDR" || dataLength != 13)
                {
                    return false;
                }

                width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(dataOffset, 4));
                height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(dataOffset + 4, 4));
                hasHeader = true;
            }
            else if (chunkType == "IHDR")
            {
                return false;
            }

            if (chunkType == "IDAT")
            {
                hasImageData = true;
            }

            offset = crcOffset + 4;
            if (chunkType == "IEND")
            {
                hasEnd = dataLength == 0 && offset == bytes.Length;
                break;
            }
        }

        return hasHeader && hasImageData && hasEnd && HasSafeDimensions(width, height);
    }

    private static bool IsJpeg(byte[] bytes)
    {
        if (bytes.Length < 12 || bytes[0] != 0xff || bytes[1] != 0xd8
            || bytes[^2] != 0xff || bytes[^1] != 0xd9)
        {
            return false;
        }

        var offset = 2;
        var hasDimensions = false;
        var hasScan = false;
        while (offset < bytes.Length - 2)
        {
            if (bytes[offset++] != 0xff)
            {
                return false;
            }

            while (offset < bytes.Length && bytes[offset] == 0xff)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                return false;
            }

            var marker = bytes[offset++];
            if (marker == 0xd9)
            {
                break;
            }

            if (marker is 0x01 or >= 0xd0 and <= 0xd7)
            {
                continue;
            }

            if (offset + 2 > bytes.Length)
            {
                return false;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > bytes.Length)
            {
                return false;
            }

            if (marker is >= 0xc0 and <= 0xc3
                or >= 0xc5 and <= 0xc7
                or >= 0xc9 and <= 0xcb
                or >= 0xcd and <= 0xcf)
            {
                if (segmentLength < 8)
                {
                    return false;
                }

                var height = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 5, 2));
                if (!HasSafeDimensions(width, height))
                {
                    return false;
                }

                hasDimensions = true;
            }

            if (marker == 0xda)
            {
                hasScan = hasDimensions;
                break;
            }

            offset += segmentLength;
        }

        return hasDimensions && hasScan;
    }

    private static bool IsWebp(byte[] bytes)
    {
        if (bytes.Length < 26
            || !bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            || !bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)
            || BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4)) != bytes.Length - 8)
        {
            return false;
        }

        var offset = 12;
        var hasDimensions = false;
        var hasImageData = false;
        uint width = 0;
        uint height = 0;
        while (offset <= bytes.Length - 8)
        {
            var chunkType = Encoding.ASCII.GetString(bytes, offset, 4);
            var chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4, 4));
            if (chunkLength > int.MaxValue || offset + 8L + chunkLength > bytes.Length)
            {
                return false;
            }

            var data = bytes.AsSpan(offset + 8, (int)chunkLength);
            if (chunkType == "VP8X" && data.Length >= 10)
            {
                width = (uint)(1 + ReadUInt24LittleEndian(data.Slice(4, 3)));
                height = (uint)(1 + ReadUInt24LittleEndian(data.Slice(7, 3)));
                hasDimensions = HasSafeDimensions(width, height);
            }

            if (chunkType == "VP8L" && data.Length >= 5 && data[0] == 0x2f)
            {
                width = (uint)(1 + data[1] + ((data[2] & 0x3f) << 8));
                height = (uint)(1 + (data[2] >> 6) + (data[3] << 2) + ((data[4] & 0x0f) << 10));
                hasDimensions = HasSafeDimensions(width, height);
                hasImageData = hasDimensions;
            }

            if (chunkType == "VP8 " && data.Length >= 10
                && data[3] == 0x9d && data[4] == 0x01 && data[5] == 0x2a)
            {
                width = (uint)(BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6, 2)) & 0x3fff);
                height = (uint)(BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8, 2)) & 0x3fff);
                hasDimensions = HasSafeDimensions(width, height);
                hasImageData = hasDimensions;
            }

            offset += 8 + (int)chunkLength + ((int)chunkLength & 1);
        }

        return hasDimensions && hasImageData;
    }

    private static uint ReadPngCrc(ReadOnlySpan<byte> bytes)
    {
        var crc = uint.MaxValue;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
            }
        }

        return ~crc;
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> bytes) =>
        bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);

    private static bool HasSafeDimensions(uint width, uint height) =>
        width is > 0 and <= 10_000
        && height is > 0 and <= 10_000
        && (ulong)width * height <= 40_000_000;
}
