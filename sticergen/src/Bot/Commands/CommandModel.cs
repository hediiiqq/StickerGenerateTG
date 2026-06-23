namespace sticergen.Bot.Commands;

public class CommandModel
{
    public TelegramCommands Type { get; set; }
    public string Arguments { get; set; } =  string.Empty;
}