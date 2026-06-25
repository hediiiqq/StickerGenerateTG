namespace sticergen.Services;

public interface IImageGenerationProvider
{
    string ProviderName { get; }

    Task<string> GenerateImageAsync(string inputImagePath, string stylePrompt, string model,
        CancellationToken cancellationToken);
}
