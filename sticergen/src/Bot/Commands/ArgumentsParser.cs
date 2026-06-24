using sticergen.Bot.Models;

namespace sticergen.Bot.Commands;

public class ArgumentsParser
{
    public NewPackCommandArgs ParseNewPack(string command)
    {
        // Ожидаемый формат: /newpack <тип стикера> <стиль> <название пака>.
        var allArgs = command.Split(" ");
        return new NewPackCommandArgs()
        {
            StickerType = allArgs[0],
            Style = allArgs[1],
            // Название пака может состоять из нескольких слов, поэтому склеиваем все оставшиеся аргументы.
            PackTitle = string.Join(" ", allArgs.Skip(2)),
        };
    }

    public AddStickerCommandArgs ParseAddSticker(string command)
    {
        // Ожидаемый формат: /addsticker <имя пака> <стиль>.
        var allArgs = command.Split(" ");
        return new AddStickerCommandArgs()
        {
            PackName =  allArgs[0],
            Style =   allArgs[1],
        };
    }
}
