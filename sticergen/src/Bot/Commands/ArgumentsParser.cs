using sticergen.Bot.Models;

namespace sticergen.Bot.Commands;

public class ArgumentsParser
{
    public NewPackCommandArgs ParseNewPack(string command)
    {
        var allArgs = SplitArguments(command);

        return new NewPackCommandArgs()
        {
            StickerType = allArgs.ElementAtOrDefault(0) ?? string.Empty,
            Style = allArgs.ElementAtOrDefault(1) ?? string.Empty,
            PackTitle = string.Join(" ", allArgs.Skip(2)),
        };
    }

    public AddStickerCommandArgs ParseAddSticker(string command)
    {
        var allArgs = SplitArguments(command);

        return new AddStickerCommandArgs()
        {
            PackName = allArgs.ElementAtOrDefault(0) ?? string.Empty,
            Style = allArgs.ElementAtOrDefault(1) ?? string.Empty,
        };
    }

    private static string[] SplitArguments(string command)
    {
        return command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
