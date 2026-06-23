using sticergen.Bot.Models;

namespace sticergen.Bot.Commands;

public class CommandParser
{
    public CommandModel Parse(string command)
    {
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