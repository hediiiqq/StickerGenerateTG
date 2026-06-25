using Microsoft.Extensions.Hosting;
using SkiaSharp;

namespace sticergen.Services;

public class ImageProcessingService
{
    private const int StickerSize = 512;

    private readonly IHostEnvironment _env;

    public ImageProcessingService(IHostEnvironment env)
    {
        _env = env;
    }

    public Task<string> RawImage(string filePath, int draftId, CancellationToken cancellationToken)
    {
        var directoryPath = Path.Combine(GetStorageRootPath(), "storage", "final");
        Directory.CreateDirectory(directoryPath);

        var finalFilePath = Path.Combine(directoryPath, $"{draftId}.png");

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
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.OpenWrite(finalFilePath);

        data.SaveTo(output);

        return Task.FromResult(finalFilePath);
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
