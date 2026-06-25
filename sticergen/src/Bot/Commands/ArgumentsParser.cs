using sticergen.Bot.Models;

namespace sticergen.Bot.Commands;

public class ArgumentsParser
{
    public NewPackCommandArgs ParseNewPack(string command)
    {
        var allArgs = SplitArguments(command);

        var stickerType = allArgs.ElementAtOrDefault(0) ?? string.Empty;
        var style = allArgs.ElementAtOrDefault(1) ?? string.Empty;
        var tail = string.Join(" ", allArgs.Skip(2));

        var args = new NewPackCommandArgs
        {
            StickerType = stickerType,
            Style = style,
        };

        if (style == "ai")
        {
            var parts = tail.Split('|', 2, StringSplitOptions.TrimEntries);

            args.PackTitle = parts.ElementAtOrDefault(0) ?? string.Empty;
            args.StylePrompt = parts.ElementAtOrDefault(1) ?? string.Empty;

            return args;
        }

        args.PackTitle = tail;
        return args;
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
