

namespace sticergen.Bot.Models;

public class BotCommandContext
{
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public CommandModel Command { get; set; } = new();
    public bool HasPhoto { get; set; }
    public string? PhotoFileId { get; set; }
    public List<string> PhotoFileIds { get; set; } = new();
}
