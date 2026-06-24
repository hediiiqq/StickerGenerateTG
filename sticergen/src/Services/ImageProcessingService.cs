using Microsoft.Extensions.Hosting;
using SkiaSharp;


namespace sticergen.Services;

public class ImageProcessingService
{
    private readonly IHostEnvironment _env;

    public ImageProcessingService(IHostEnvironment env)
    {
        _env = env;
    }

    public Task<string> RawImage(string filePath, int draftId, CancellationToken cancellationToken)
    {
        var rootPath = _env.ContentRootPath;

        // При запуске из bin/Debug/... возвращаемся к корню проекта, чтобы storage лежал рядом с исходниками.
        var binIndex = rootPath.IndexOf(
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);

        if (binIndex >= 0)
        {
            rootPath = rootPath[..binIndex];
        }

        var directoryPath = Path.Combine(rootPath, "storage", "final");
        Directory.CreateDirectory(directoryPath);

        var finalFilePath = Path.Combine(directoryPath, $"{draftId}.png");

        using var sourceBitmap = SKBitmap.Decode(filePath);

        // Telegram-стикеры готовим на прозрачном квадратном холсте 512x512 пикселей.
        using var surface = SKSurface.Create(new SKImageInfo(512, 512));
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        // Выбираем меньший масштаб, чтобы вся фотография поместилась в квадрат без обрезки.
        var scale = Math.Min(
            512f / sourceBitmap.Width,
            512f / sourceBitmap.Height);

        var newWidth = sourceBitmap.Width * scale;
        var newHeight = sourceBitmap.Height * scale;

        // Центрируем уменьшенное изображение внутри квадратного холста.
        var left = (512 - newWidth) / 2;
        var top = (512 - newHeight) / 2;

        var destRect = new SKRect(
            left,
            top,
            left + newWidth,
            top + newHeight);

        using var paint = new SKPaint
        {
            IsAntialias = true
        };

        canvas.DrawBitmap(sourceBitmap, destRect, paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.OpenWrite(finalFilePath);

        // Сохраняем готовое превью в storage/final и возвращаем путь к файлу.
        data.SaveTo(output);

        return Task.FromResult(finalFilePath);
    }
}
