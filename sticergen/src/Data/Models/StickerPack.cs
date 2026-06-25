namespace sticergen.Data.Models;

public class StickerPack
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public string PackName { get; set; } = string.Empty;
    public string PackTitle { get; set; } = string.Empty;
    public string StickerType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

}