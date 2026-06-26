namespace sticergen.Services;

public static class ImageGenerationModelCatalog
{
    public static readonly IReadOnlyDictionary<string, string[]> ModelsByProvider =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["stability"] =
            [
                "sd3.5-medium",
                "sd3.5-large",
                "sd3.5-large-turbo"
            ],
            ["cloudflare"] =
            [
                "@cf/runwayml/stable-diffusion-v1-5-img2img"
            ]
        };

    public static bool IsSupported(string provider, string model)
    {
        return ModelsByProvider.TryGetValue(provider, out var models)
            && models.Contains(model, StringComparer.OrdinalIgnoreCase);
    }

    public static string FormatSupportedModels()
    {
        return string.Join(
            '\n',
            ModelsByProvider.Select(x => $"{x.Key}: {string.Join(", ", x.Value)}"));
    }
}
