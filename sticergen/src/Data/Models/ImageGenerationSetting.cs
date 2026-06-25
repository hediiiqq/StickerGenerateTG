namespace sticergen.Data.Models;

public class ImageGenerationSetting
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
