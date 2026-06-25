using sticergen.Bot.Models;
using sticergen.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace sticergen.Bot.Commands;

public class CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ArgumentsParser _parser;
    private readonly DraftService _draftService;
    private readonly FileStorageService _fileService;
    private readonly ImageProcessingService _imageProcess;
    private readonly StickerPackService _stickerPack;

    public CommandHandler(ITelegramBotClient botClient, ArgumentsParser parser, DraftService draftService,
        FileStorageService fileService, ImageProcessingService imageProcess, StickerPackService stickerPack)
    {
        _botClient = botClient;
        _parser = parser;
        _draftService = draftService;
        _fileService = fileService;
        _imageProcess = imageProcess;
        _stickerPack = stickerPack;
    }

    public async Task HandleAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        // CommandHandler отвечает за реакцию бота после того, как CommandParser уже распознал команду.
        switch (context.Command.Type)
        {
            case TelegramCommands.Start:
            {
                await _botClient.SendMessage(context.ChatId,
                    "Привет! Я помогу создать набор стикеров из ваших фотографий." +
                    "\n Используйте /help, чтобы посмотреть список команд.", cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Help:
            {
                await _botClient.SendMessage(context.ChatId,
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
                // Получаем все черновики текущего пользователя, чтобы показать их в /mypacks.
                var mypacks = await _stickerPack.GetStickerPacksAsync(context.UserId, stoppingToken);
                {
                    var message = "Ваши стикерпаки:\n";
                    foreach (var mypack in mypacks)
                    {
                        message += $"{mypack.PackTitle} - {mypack.StickerType} - https://t.me/addstickers/{mypack.PackName}\n";
                    }

                    await _botClient.SendMessage(context.ChatId, message, cancellationToken: stoppingToken);
                }

                break;
            }
            case TelegramCommands.Newpack:
            {
                // Аргументы команды отделены от имени команды и разбираются отдельно.
                var args = _parser.ParseNewPack(context.Command.Arguments);

                // Новый стикерпак создаётся только при наличии фотографии.
                if (!context.HasPhoto)
                {
                    await _botClient.SendMessage(
                        context.ChatId,
                        "Прикрепи фото к команде или отправь команду ответом на фото.",
                        cancellationToken: stoppingToken);

                    break;
                }

                var draft = await _draftService.CreateNewPackDraftAsync(
                    context.UserId,
                    context.ChatId,
                    context.PhotoFileId,
                    args,
                    stoppingToken);

                if (context.PhotoFileId != null)
                {
                    var originalFilePath =
                        await _fileService.SaveOriginalPhotoAsync(context.PhotoFileId, draft.Id, stoppingToken);
                    Console.WriteLine(originalFilePath);
                    // RawImage подготавливает превью стикера и возвращает путь к готовому PNG-файлу.
                    var finalFilePath = await _imageProcess.RawImage(originalFilePath, draft.Id, stoppingToken);

                    await _draftService.UpdateDraftStickerFilePathsAsync( draft.Id,originalFilePath, finalFilePath, stoppingToken);

                    await using var previewStream = File.OpenRead(finalFilePath);

                    var stickerLink = await _stickerPack.CreateStickerPackAsync(draft.Id, stoppingToken);

                    await _botClient.SendPhoto(
                        context.ChatId,
                        InputFile.FromStream(stream: previewStream, fileName: "preview.png"),
                        caption: "Preview raw 512x512",
                        cancellationToken: stoppingToken);
                    await _botClient.SendMessage(
                        context.ChatId,
                        $"newpack\n stickertype:{args.StickerType}\n" +
                        $" style:{args.Style}\n packtitle:{args.PackTitle}\n " +
                        $"photo:{context.HasPhoto}\n fileid:{context.PhotoFileId}\n {finalFilePath}\n" +
                        $"link :{stickerLink}",
                        cancellationToken: stoppingToken);
                }

                break;
            }
            case TelegramCommands.Addsticker:
            {
                // Для добавления стикера пока разбираем только имя пака и стиль.
                var args = _parser.ParseAddSticker(context.Command.Arguments);

                var parsPack =  await _stickerPack.ParseStickerPackAsync(args.PackName, context.UserId, stoppingToken);

                if (parsPack == null)
                {
                    await _botClient.SendMessage(context.ChatId, "пак не найден", cancellationToken: stoppingToken);
                    break;
                }

                if (!context.HasPhoto)
                {
                    await _botClient.SendMessage(context.ChatId,"Прикрепи фото",cancellationToken: stoppingToken);
                    break;
                }



                await _botClient.SendMessage(context.ChatId,
                    $"addsticker\n packname:{args.PackName}\n style:{args.Style}",
                    cancellationToken: stoppingToken);
                break;
            }
            case TelegramCommands.Unknown:
            {
                await _botClient.SendMessage(context.ChatId,
                    "все доступные мне команды /help, остальные я не понимаю",
                    cancellationToken: stoppingToken);
                break;
            }
            default:
            {
                await _botClient.SendMessage(context.ChatId,
                    "все доступные мне команды /help, остальные я не понимаю",
                    cancellationToken: stoppingToken);
                break;
            }
        }
    }
}
