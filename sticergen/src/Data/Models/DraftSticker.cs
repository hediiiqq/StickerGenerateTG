namespace sticergen.Data.Models;

public class DraftSticker
{
    public int Id { get; set; }
    public int DraftId { get; set; }
    public Draft Draft { get; set; } = null!;
    public string TelegramFileId { get; set; } = string.Empty;
    public string OriginalFilePath { get; set; } = string.Empty;
    public string FilnalFilePath { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🤔";
    public int SortOrder { get; set; }
}