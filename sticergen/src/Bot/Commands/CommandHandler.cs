using sticergen.Bot.Models;
using Telegram.Bot;

namespace sticergen.Bot.Commands;

public class CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ArgumentsParser _parser;

    public CommandHandler(ITelegramBotClient botClient, ArgumentsParser parser)
    {
        _botClient = botClient;
        _parser = parser;
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
                await _botClient.SendMessage(chatId, $"mypacks:{command.Arguments}", cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Newpack:
            {
                var args = _parser.ParseNewPack(command.Arguments);
                await _botClient.SendMessage(chatId,
                    $"newpack\n stickertype:{args.StickerType}\n style:{args.Style}\n packtitle:{args.PackTitle}",
                    cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Addsticker:
            {
                var args = _parser.ParseAddSticker(command.Arguments);
                await _botClient.SendMessage(chatId, $"addsticker\n packname:{args.PackName}\n style:{args.Style}",
                    cancellationToken: stoppingToken);
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