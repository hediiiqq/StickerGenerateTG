using sticergen.Bot.Models;

namespace sticergen.Bot.Commands;

public class ArgumentsParser
{
    public NewPackCommandArgs ParseNewPack(string command)
    {
        var allArgs = command.Split(" ");
        return new NewPackCommandArgs()
        {
            StickerType = allArgs[0],
            Style = allArgs[1],
            PackTitle = string.Join(" ", allArgs.Skip(2)),
        };
    }

    public AddStickerCommandArgs ParseAddSticker(string command)
    {
        var allArgs = command.Split(" ");
        return new AddStickerCommandArgs()
        {
            PackName =  allArgs[0],
            Style =   allArgs[1],
        };
    }
}