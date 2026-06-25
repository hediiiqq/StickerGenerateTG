namespace sticergen.Bot.Models;

public class NewPackCommandArgs
{
    public string StickerType { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string StylePrompt { get; set; } = string.Empty;
    public string PackTitle { get; set; } = string.Empty;
}