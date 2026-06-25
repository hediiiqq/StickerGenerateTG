using sticergen.Bot.Models;
using sticergen.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace sticergen.Bot.Commands;

public class CommandHandler
{
    private const string PreviewFileName = "preview.png";
    private const string DefaultStickerEmoji = "🤔";

    private readonly ITelegramBotClient _botClient;
    private readonly ArgumentsParser _parser;
    private readonly DraftService _draftService;
    private readonly FileStorageService _fileService;
    private readonly ImageProcessingService _imageProcess;
    private readonly StickerPackService _stickerPack;

    public CommandHandler(
        ITelegramBotClient botClient,
        ArgumentsParser parser,
        DraftService draftService,
        FileStorageService fileService,
        ImageProcessingService imageProcess,
        StickerPackService stickerPack)
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
        switch (context.Command.Type)
        {
            case TelegramCommands.Start:
                await SendStartAsync(context, stoppingToken);
                break;

            case TelegramCommands.Help:
                await SendHelpAsync(context, stoppingToken);
                break;

            case TelegramCommands.Mypacks:
                await SendMyPacksAsync(context, stoppingToken);
                break;

            case TelegramCommands.Newpack:
                await HandleNewPackAsync(context, stoppingToken);
                break;

            case TelegramCommands.Addsticker:
                await HandleAddStickerAsync(context, stoppingToken);
                break;

            case TelegramCommands.Unknown:
            default:
                await SendUnknownAsync(context, stoppingToken);
                break;
        }
    }

    private async Task SendStartAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        await _botClient.SendMessage(context.ChatId, "start", cancellationToken: stoppingToken);
    }

    private async Task SendHelpAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        await _botClient.SendMessage(
            context.ChatId,
            "/start — запустить бота\n" +
            "/help — показать помощь\n" +
            "/newpack static raw Название пака — создать черновик нового пака\n" +
            "/newpack static outline Название пака — создать черновик с outline-стилем\n" +
            "/addsticker pack_name raw — добавить стикер в существующий пак\n" +
            "/mypacks — показать мои черновики\n\n" +
            "Фото можно отправить с подписью-командой или написать команду ответом на фото.",
            cancellationToken: stoppingToken);
    }

    private async Task SendMyPacksAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        var packs = await _stickerPack.GetStickerPacksAsync(context.UserId, stoppingToken);
        var packLines = packs.Select(pack =>
            $"{pack.PackTitle} - {pack.StickerType} - https://t.me/addstickers/{pack.PackName}");

        var message = "your packs:\n" + string.Join('\n', packLines);

        await _botClient.SendMessage(context.ChatId, message, cancellationToken: stoppingToken);
    }

    private async Task HandleNewPackAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        var args = _parser.ParseNewPack(context.Command.Arguments);
        var photoFileId = context.PhotoFileId;

        if (string.IsNullOrWhiteSpace(args.StickerType) ||
            string.IsNullOrWhiteSpace(args.Style) ||
            string.IsNullOrWhiteSpace(args.PackTitle))
        {
            await _botClient.SendMessage(
                context.ChatId,
                "Формат: /newpack static raw Название пака",
                cancellationToken: stoppingToken);

            return;
        }

        if (!context.HasPhoto || photoFileId is null)
        {
            await _botClient.SendMessage(
                context.ChatId,
                "Прикрепи фото к команде или отправь команду ответом на фото.",
                cancellationToken: stoppingToken);

            return;
        }

        var draft = await _draftService.CreateNewPackDraftAsync(
            context.UserId,
            context.ChatId,
            photoFileId,
            args,
            stoppingToken);

        var originalFilePath = await _fileService.SaveOriginalPhotoAsync(photoFileId, draft.Id, stoppingToken);
        var finalFilePath = await _imageProcess.RawImage(originalFilePath, draft.Id, stoppingToken);

        await _draftService.UpdateDraftStickerFilePathsAsync(
            draft.Id,
            originalFilePath,
            finalFilePath,
            stoppingToken);

        var stickerLink = await _stickerPack.CreateStickerPackAsync(draft.Id, stoppingToken);

        await SendStickerPreviewAsync(context.ChatId, finalFilePath, stoppingToken);
        await _botClient.SendMessage(
            context.ChatId,
            $"newpack\n stickertype:{args.StickerType}\n" +
            $" style:{args.Style}\n packtitle:{args.PackTitle}\n " +
            $"photo:{context.HasPhoto}\n fileid:{photoFileId}\n {finalFilePath}\n" +
            $"link :{stickerLink}",
            cancellationToken: stoppingToken);
    }

    private async Task HandleAddStickerAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        var args = _parser.ParseAddSticker(context.Command.Arguments);

        if (string.IsNullOrWhiteSpace(args.PackName) ||
            string.IsNullOrWhiteSpace(args.Style))
        {
            await _botClient.SendMessage(
                context.ChatId,
                "Формат: /addsticker pack_name raw",
                cancellationToken: stoppingToken);

            return;
        }

        var pack = await _stickerPack.ParseStickerPackAsync(args.PackName, context.UserId, stoppingToken);

        if (pack is null)
        {
            await _botClient.SendMessage(context.ChatId, "пак не найден", cancellationToken: stoppingToken);
            return;
        }

        var photoFileId = context.PhotoFileId;

        if (!context.HasPhoto || photoFileId is null)
        {
            await _botClient.SendMessage(context.ChatId, "Прикрепи фото", cancellationToken: stoppingToken);
            return;
        }

        var draft = await _draftService.CreateAddStickerDraftAsync(
            context.UserId,
            context.ChatId,
            photoFileId,
            args,
            pack,
            stoppingToken);

        var originalFilePath = await _fileService.SaveOriginalPhotoAsync(photoFileId, draft.Id, stoppingToken);
        var finalFilePath = await _imageProcess.RawImage(originalFilePath, draft.Id, stoppingToken);

        await _draftService.UpdateDraftStickerFilePathsAsync(
            draft.Id,
            originalFilePath,
            finalFilePath,
            stoppingToken);

        await SendStickerPreviewAsync(context.ChatId, finalFilePath, stoppingToken);
        await _botClient.SendMessage(
            context.ChatId,
            "стикер готов к добавлению",
            cancellationToken: stoppingToken);

        var stickerLink = await _stickerPack.AddStickerToPackAsync(
            pack.PackName,
            context.UserId,
            finalFilePath,
            DefaultStickerEmoji,
            stoppingToken);

        await _botClient.SendMessage(
            context.ChatId,
            $"link : {stickerLink}",
            cancellationToken: stoppingToken);
    }

    private async Task SendStickerPreviewAsync(
        long chatId,
        string finalFilePath,
        CancellationToken stoppingToken)
    {
        await using var previewStream = File.OpenRead(finalFilePath);

        await _botClient.SendPhoto(
            chatId,
            InputFile.FromStream(stream: previewStream, fileName: PreviewFileName),
            caption: "Preview raw 512x512",
            cancellationToken: stoppingToken);
    }

    private async Task SendUnknownAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        await _botClient.SendMessage(context.ChatId, "unknown", cancellationToken: stoppingToken);
    }
}
