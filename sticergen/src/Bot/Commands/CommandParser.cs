using sticergen.Bot.Models;

namespace sticergen.Bot.Commands;

public class CommandParser
{
    public CommandModel Parse(string command)
    {
        // Пустой текст нельзя разобрать как команду, поэтому сразу возвращаем Unknown.
        if (string.IsNullOrWhiteSpace(command))
        {
            return new CommandModel()
            {
                Type = TelegramCommands.Unknown,
            };
        }

        var firstSpaceIndex = command.IndexOf(' ');

        ReadOnlySpan<char> firstWord;
        string arguments;

        // Первая часть строки — имя команды, всё после первого пробела — аргументы команды.
        if (firstSpaceIndex == -1)
        {
            firstWord = command.TrimStart("/");
            arguments = string.Empty;
        }
        else
        {
            firstWord = command.Substring(0, firstSpaceIndex).TrimStart("/");
            arguments = command.Substring(firstSpaceIndex + 1);
        }

        // Здесь текстовая команда Telegram сопоставляется с внутренним enum TelegramCommands.
        switch (firstWord)
        {
            case "start":
                return new CommandModel()
                {
                    Type = TelegramCommands.Start
                };

            case "help":
                return new CommandModel()
                {
                    Type = TelegramCommands.Help
                };

            case "mypacks":
                return new CommandModel()
                {
                    Type = TelegramCommands.Mypacks
                };
            case "aimodel":
                return new CommandModel()
                {
                    Type = TelegramCommands.Aimodel,
                    Arguments = arguments,
                };
            case "aimodels":
                return new CommandModel()
                {
                    Type = TelegramCommands.Aimodels
                };
            case "aistatus":
                return new CommandModel()
                {
                    Type = TelegramCommands.Aistatus
                };
            case "newpack":
                return new CommandModel()
                {
                    Type = TelegramCommands.Newpack,
                    Arguments = arguments,
                };

            case "addsticker":
                return new CommandModel()
                {
                    Type = TelegramCommands.Addsticker,
                    Arguments = arguments,
                };

            default:
                return new CommandModel()
                {
                    Type = TelegramCommands.Unknown,
                    Arguments = arguments,
                };
        }
    }
}
