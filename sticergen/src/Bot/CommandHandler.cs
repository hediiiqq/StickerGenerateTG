using sticergen.Bot.Commands;
using Telegram.Bot;


namespace sticergen.Bot;

public class CommandHandler
{
    private readonly ITelegramBotClient _botClient;

    public CommandHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task HandleAsync(long chatId, CommandModel command, CancellationToken stoppingToken)
    {
        switch (command.Type)
        {
            case TelegramCommands.Start:
            {
                await _botClient.SendMessage(chatId, "start", cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Help:
            {
                await _botClient.SendMessage(chatId, "help", cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Mypacks:
            {
                await _botClient.SendMessage(chatId, "mypacks", cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Newpack:
            {
                await _botClient.SendMessage(chatId, "newpack", cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Addsticker:
            {
                await _botClient.SendMessage(chatId, "addsticker", cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Unknown:
            {
                await _botClient.SendMessage(chatId, "unknown", cancellationToken: stoppingToken);
                break;
            }
            default:
            {
                await _botClient.SendMessage(chatId, "unknown", cancellationToken: stoppingToken);
                break;
            }
        }
    }
}