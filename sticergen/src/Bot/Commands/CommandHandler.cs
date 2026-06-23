using sticergen.Bot.Models;
using sticergen.Services;
using Telegram.Bot;

namespace sticergen.Bot.Commands;

public class CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ArgumentsParser _parser;
    private readonly DraftService _draftService;

    public CommandHandler(ITelegramBotClient botClient, ArgumentsParser parser, DraftService draftService)
    {
        _botClient = botClient;
        _parser = parser;
        _draftService = draftService;
    }

    public async Task HandleAsync(long userId, long chatId, CommandModel command, CancellationToken stoppingToken)
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
                await _botClient.SendMessage(chatId,
                    "/start — запустить бота\n" +
                    "/help — показать помощь\n" +
                    "/newpack static raw Название пака — создать черновик нового пака\n" +
                    "/newpack static outline Название пака — создать черновик с outline-стилем\n" +
                    "/addsticker pack_name raw — добавить стикер в существующий пак\n" +
                    "/mypacks — показать мои черновики\n\n" +
                    "Фото можно отправить с подписью-командой или написать команду ответом на фото.",
                cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Mypacks:
            {
                var mypacks = await _draftService.GetUserDraftsAsync(userId, stoppingToken);
                if (mypacks is null)
                {
                    await _botClient.SendMessage(chatId, "mypacks is empty", cancellationToken: stoppingToken);
                }
                else
                {
                    var message = "your packs:\n";
                    foreach (var mypack in mypacks)
                    {
                        message += $"#{mypack.Mode} {mypack.Status} - {mypack.PackTitle}\n";
                    }

                    await _botClient.SendMessage(chatId, message, cancellationToken: stoppingToken);
                }

                break;
            }
            case TelegramCommands.Newpack:
            {
                var args = _parser.ParseNewPack(command.Arguments);
                await _draftService.CreateNewPackDraftAsync(userId, chatId, args, stoppingToken);
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