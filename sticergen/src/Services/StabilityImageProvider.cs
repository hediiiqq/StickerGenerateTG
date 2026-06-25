using System.Net.Http.Headers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SkiaSharp;
using sticergen.Configuration;

namespace sticergen.Services;

public class StabilityImageProvider : IImageGenerationProvider
{
    private const int ImageSize = 1024;
    private const string Endpoint = "https://api.stability.ai/v2beta/stable-image/generate/sd3";
    public string ProviderName => "stability";

    private static readonly IReadOnlyDictionary<string, StabilityModelProfile> ModelProfiles =
        new Dictionary<string, StabilityModelProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["sd3.5-medium"] = new("0.68"),
            ["sd3.5-large"] = new("0.62"),
            ["sd3.5-large-turbo"] = new("0.72")
        };

    private readonly ImageGenerationOptions _options;
    private readonly HttpClient _httpClient;
    private readonly IHostEnvironment _env;

    public StabilityImageProvider(IOptions<ImageGenerationOptions> options, HttpClient httpClient, IHostEnvironment env)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _env = env;
    }

    public async Task<string> GenerateImageAsync(
        string inputImagePath,
        string stylePrompt,
        string model,
        CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Stability API key is missing.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("Stability image model is missing.");
        }

        if (model.StartsWith("@cf/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Stability provider is configured with a Cloudflare model. Use /aimodel stability sd3.5-medium.");
        }

        if (string.IsNullOrWhiteSpace(inputImagePath) || !File.Exists(inputImagePath))
        {
            throw new InvalidOperationException("Input image file not found.");
        }

        if (string.IsNullOrWhiteSpace(stylePrompt))
        {
            throw new InvalidOperationException("Style prompt is empty.");
        }

        var imageBytes = PrepareInputImageBytes(inputImagePath);
        var profile = GetModelProfile(model);

        using var form = new MultipartFormDataContent();
        AddStringPart(form, "model", model);
        AddStringPart(form, "mode", "image-to-image");
        AddStringPart(form, "prompt", ImagePromptBuilder.BuildImageToImageStickerPrompt(stylePrompt, model));
        AddStringPart(form, "negative_prompt", ImagePromptBuilder.StickerNegativePrompt);
        AddStringPart(form, "strength", profile.Strength);
        AddStringPart(form, "output_format", "png");
        AddFilePart(form, "image", "input.png", imageBytes, "image/png");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

        using var response = await _httpClient.PostAsync(Endpoint, form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Stability image generation failed: {errorText}");
        }

        var generatedBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var directoryPath = Path.Combine(GetStorageRootPath(), "storage", "generated");
        Directory.CreateDirectory(directoryPath);

        var generatedFilePath = Path.Combine(
            directoryPath,
            $"generated-{Guid.NewGuid():N}.png");

        await File.WriteAllBytesAsync(generatedFilePath, generatedBytes, cancellationToken);

        return generatedFilePath;
    }

    private static StabilityModelProfile GetModelProfile(string model)
    {
        return ModelProfiles.TryGetValue(model, out var profile)
            ? profile
            : new StabilityModelProfile("0.68");
    }

    private static void AddStringPart(MultipartFormDataContent form, string name, string value)
    {
        var content = new StringContent(value);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = QuoteHeaderValue(name)
        };

        form.Add(content);
    }

    private static void AddFilePart(
        MultipartFormDataContent form,
        string name,
        string fileName,
        byte[] bytes,
        string contentType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = QuoteHeaderValue(name),
            FileName = QuoteHeaderValue(fileName)
        };

        form.Add(content);
    }

    private static string QuoteHeaderValue(string value)
    {
        return $"\"{value}\"";
    }

    private string GetApiKey()
    {
        return !string.IsNullOrWhiteSpace(_options.StabilityApiKey)
            ? _options.StabilityApiKey
            : _options.ApiKey;
    }

    private static byte[] PrepareInputImageBytes(string inputImagePath)
    {
        using var sourceBitmap = SKBitmap.Decode(inputImagePath);
        if (sourceBitmap is null)
        {
            throw new InvalidOperationException($"Cannot decode input image file: {inputImagePath}");
        }

        using var surface = SKSurface.Create(new SKImageInfo(ImageSize, ImageSize));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var scale = Math.Max(
            (float)ImageSize / sourceBitmap.Width,
            (float)ImageSize / sourceBitmap.Height);

        var newWidth = sourceBitmap.Width * scale;
        var newHeight = sourceBitmap.Height * scale;
        var left = (ImageSize - newWidth) / 2;
        var top = (ImageSize - newHeight) / 2;

        using var paint = new SKPaint
        {
            IsAntialias = true
        };

        canvas.DrawBitmap(
            sourceBitmap,
            new SKRect(left, top, left + newWidth, top + newHeight),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None),
            paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Cannot encode input image for Stability.");

        return data.ToArray();
    }

    private string GetStorageRootPath()
    {
        var rootPath = _env.ContentRootPath;
        var binIndex = rootPath.IndexOf(
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);

        return binIndex >= 0 ? rootPath[..binIndex] : rootPath;
    }

    private sealed record StabilityModelProfile(string Strength);
}
