namespace sticergen.Data.Models;

public class Draft
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string PackTitle { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string StickerType { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public List<DraftSticker> Stickers { get; set; } = new();
}