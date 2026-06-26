namespace sticergen.Services;

public class ImageGenerationService
{
    private readonly ImageProcessingService _imageProcessing;
    private readonly IEnumerable<IImageGenerationProvider> _imageProviders;
    private readonly ImageGenerationSettingsService _settings;

    public ImageGenerationService(
        ImageProcessingService imageProcessing,
        IEnumerable<IImageGenerationProvider> imageProviders,
        ImageGenerationSettingsService settings)
    {
        _imageProcessing = imageProcessing;
        _imageProviders = imageProviders;
        _settings = settings;
    }

    public async Task<string> PrepareStickerImageAsync(string originalFilePath, int draftId, int stickerId, string style,
        string stylePrompt, CancellationToken cancellationToken)
    {
        var normalizedStyle = style.ToLowerInvariant();
        if (normalizedStyle == "raw")
        {
            var raw = await _imageProcessing.RawImage(originalFilePath, draftId, stickerId, cancellationToken);
            return raw;
        }

        if (normalizedStyle == "ai")
        {
            if (string.IsNullOrWhiteSpace(stylePrompt))
                throw new InvalidOperationException("AI style prompt is empty.");
            ;

            var activeSetting = await _settings.GetActiveAsync(cancellationToken);
            var provider = _imageProviders.FirstOrDefault(
                    x => string.Equals(x.ProviderName, activeSetting.Provider, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Unknown image generation provider: {activeSetting.Provider}");

            var ai = await provider.GenerateImageAsync(
                originalFilePath,
                stylePrompt,
                activeSetting.Model,
                cancellationToken);

            var aiDone = await _imageProcessing.RawImage(ai, draftId, stickerId, cancellationToken);
            return aiDone;
        }

        throw new InvalidOperationException($"Unknown image style: {style}");
    }
}
