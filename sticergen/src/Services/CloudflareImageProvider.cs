using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SkiaSharp;
using sticergen.Configuration;

namespace sticergen.Services;

public class CloudflareImageProvider : IImageGenerationProvider
{
    private const int ImageSize = 512;
    public string ProviderName => "cloudflare";

    private static readonly IReadOnlyDictionary<string, CloudflareModelProfile> ModelProfiles =
        new Dictionary<string, CloudflareModelProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["@cf/runwayml/stable-diffusion-v1-5-img2img"] = new(0.45, 9.0, 20)
        };

    private readonly ImageGenerationOptions _options;
    private readonly HttpClient _httpClient;
    private readonly IHostEnvironment _env;

    public CloudflareImageProvider(IOptions<ImageGenerationOptions> options, HttpClient httpClient,
        IHostEnvironment env)
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
        var accountId = GetAccountId();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Cloudflare API key is missing.");
        }

        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new InvalidOperationException("Cloudflare account id is missing.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("Cloudflare model is missing.");
        }

        if (string.Equals(
                model,
                "@cf/bytedance/stable-diffusion-xl-lightning",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Configured Cloudflare model does not support image-to-image input. Use @cf/runwayml/stable-diffusion-v1-5-img2img for AI sticker stylization.");
        }

        if (IsDreamShaperModel(model))
        {
            throw new InvalidOperationException(
                "Configured Cloudflare model does not support image-to-image input. Use @cf/runwayml/stable-diffusion-v1-5-img2img for AI sticker stylization.");
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
        var requestBody = BuildRequestBody(stylePrompt, imageBytes, model);

        var url = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/{model}";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsJsonAsync(
            url,
            requestBody,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Cloudflare generation failed: {errorText}");
        }

        var generatedBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var directoryPath = Path.Combine(GetStorageRootPath(), "storage", "generated");
        Directory.CreateDirectory(directoryPath);

        var generatedFilePath = Path.Combine(
            directoryPath,
            $"generated-{Guid.NewGuid():N}.png");

        await File.WriteAllBytesAsync(
            generatedFilePath,
            generatedBytes,
            cancellationToken);

        return generatedFilePath;
    }

    private string GetApiKey()
    {
        return !string.IsNullOrWhiteSpace(_options.CloudflareApiKey)
            ? _options.CloudflareApiKey
            : _options.ApiKey;
    }

    private string GetAccountId()
    {
        return !string.IsNullOrWhiteSpace(_options.CloudflareAccountId)
            ? _options.CloudflareAccountId
            : _options.AccountId;
    }

    private static object BuildRequestBody(string stylePrompt, byte[] imageBytes, string model)
    {
        var profile = GetModelProfile(model);
        var prompt = ImagePromptBuilder.BuildImageToImageStickerPrompt(stylePrompt, model);

        return new
        {
            prompt,
            negative_prompt = ImagePromptBuilder.StickerNegativePrompt,
            image = Array.ConvertAll(imageBytes, b => (int)b),
            strength = profile.Strength,
            guidance = profile.Guidance,
            num_steps = profile.NumSteps,
            width = ImageSize,
            height = ImageSize
        };
    }

    private static CloudflareModelProfile GetModelProfile(string model)
    {
        return ModelProfiles.TryGetValue(model, out var profile)
            ? profile
            : new CloudflareModelProfile(0.45, 9.0, 20);
    }

    private static bool IsDreamShaperModel(string model)
    {
        return string.Equals(
            model,
            "@cf/lykon/dreamshaper-8-lcm",
            StringComparison.OrdinalIgnoreCase);
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

        var scale = Math.Min(
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
            ?? throw new InvalidOperationException("Cannot encode input image for Cloudflare.");

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

    private sealed record CloudflareModelProfile(double Strength, double Guidance, int NumSteps);
}
