using sticergen.Bot.Commands;

namespace sticergen.Bot.Models;

public class CommandModel
{
    public TelegramCommands Type { get; set; }
    public string Arguments { get; set; } =  string.Empty;
}