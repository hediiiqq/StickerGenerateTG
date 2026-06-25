namespace sticergen.Configuration;

public class ImageGenerationOptions
{
    public string Provider { get; set; } =  string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string StabilityApiKey { get; set; } = string.Empty;
    public string CloudflareApiKey { get; set; } = string.Empty;
    public string CloudflareAccountId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
