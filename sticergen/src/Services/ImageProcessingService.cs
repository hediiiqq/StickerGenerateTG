using Microsoft.Extensions.Hosting;
using SkiaSharp;

namespace sticergen.Services;

public class ImageProcessingService
{
    private const int MaxStickerFileBytes = 512 * 1024;
    private const int StickerSize = 512;
    private static readonly int[] WebpQualitySteps = [90, 80, 70, 60, 50, 40, 30, 20];

    private readonly IHostEnvironment _env;

    public ImageProcessingService(IHostEnvironment env)
    {
        _env = env;
    }

    public Task<string> RawImage(string filePath, int draftId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directoryPath = Path.Combine(GetStorageRootPath(), "storage", "final");
        Directory.CreateDirectory(directoryPath);

        var finalFilePath = Path.Combine(directoryPath, $"{draftId}.webp");

        using var sourceBitmap = SKBitmap.Decode(filePath);
        if (sourceBitmap is null)
        {
            throw new InvalidOperationException($"Cannot decode image file: {filePath}");
        }

        using var surface = SKSurface.Create(new SKImageInfo(StickerSize, StickerSize));
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        var scale = Math.Min(
            (float)StickerSize / sourceBitmap.Width,
            (float)StickerSize / sourceBitmap.Height);

        var newWidth = sourceBitmap.Width * scale;
        var newHeight = sourceBitmap.Height * scale;
        var left = (StickerSize - newWidth) / 2;
        var top = (StickerSize - newHeight) / 2;

        var destRect = new SKRect(
            left,
            top,
            left + newWidth,
            top + newHeight);

        using var paint = new SKPaint
        {
            IsAntialias = true
        };

        canvas.DrawBitmap(
            sourceBitmap,
            destRect,
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None),
            paint);

        using var image = surface.Snapshot();
        var encodedBytes = EncodeStickerWebp(image);
        File.WriteAllBytes(finalFilePath, encodedBytes);

        return Task.FromResult(finalFilePath);
    }

    public Task<string> CreatePreviewPngAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var sourceBitmap = SKBitmap.Decode(filePath);
        if (sourceBitmap is null)
        {
            throw new InvalidOperationException($"Cannot decode preview image file: {filePath}");
        }

        using var image = SKImage.FromBitmap(sourceBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Cannot encode preview image as PNG.");

        var previewFilePath = Path.Combine(
            Path.GetDirectoryName(filePath) ?? GetStorageRootPath(),
            $"{Path.GetFileNameWithoutExtension(filePath)}-preview.png");

        File.WriteAllBytes(previewFilePath, data.ToArray());
        return Task.FromResult(previewFilePath);
    }

    private static byte[] EncodeStickerWebp(SKImage image)
    {
        foreach (var quality in WebpQualitySteps)
        {
            using var data = image.Encode(SKEncodedImageFormat.Webp, quality)
                ?? throw new InvalidOperationException("Cannot encode sticker image as WebP.");
            var bytes = data.ToArray();

            if (bytes.Length <= MaxStickerFileBytes)
            {
                return bytes;
            }
        }

        throw new InvalidOperationException(
            $"Encoded sticker is larger than Telegram limit ({MaxStickerFileBytes} bytes).");
    }

    private string GetStorageRootPath()
    {
        var rootPath = _env.ContentRootPath;
        var binIndex = rootPath.IndexOf(
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);

        return binIndex >= 0 ? rootPath[..binIndex] : rootPath;
    }
}
