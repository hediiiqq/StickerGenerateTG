using sticergen.Bot.Models;
using sticergen.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace sticergen.Bot.Commands;

public class CommandHandler
{
    private const string AiModelCallbackPrefix = "aimodel";
    private const string PreviewFileName = "preview.png";
    private const string DefaultStickerEmoji = "🤔";

    private readonly ITelegramBotClient _botClient;
    private readonly ArgumentsParser _parser;
    private readonly DraftService _draftService;
    private readonly FileStorageService _fileService;
    private readonly ImageProcessingService _imageProcess;
    private readonly StickerPackService _stickerPack;
    private readonly ImageGenerationService _imageGeneration;
    private readonly ImageGenerationSettingsService _imageSettings;

    public CommandHandler(
        ITelegramBotClient botClient,
        ArgumentsParser parser,
        DraftService draftService,
        FileStorageService fileService,
        ImageProcessingService imageProcess,
        StickerPackService stickerPack,
        ImageGenerationService imageGeneration,
        ImageGenerationSettingsService imageSettings)
    {
        _botClient = botClient;
        _parser = parser;
        _draftService = draftService;
        _fileService = fileService;
        _imageProcess = imageProcess;
        _stickerPack = stickerPack;
        _imageGeneration = imageGeneration;
        _imageSettings = imageSettings;
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

            case TelegramCommands.Aimodel:
                await HandleAiModelAsync(context, stoppingToken);
                break;

            case TelegramCommands.Aimodels:
                await SendAiModelsAsync(context, stoppingToken);
                break;

            case TelegramCommands.Aistatus:
                await SendAiStatusAsync(context, stoppingToken);
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

    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken stoppingToken)
    {
        if (callbackQuery.Message?.Chat is null)
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: stoppingToken);
            return;
        }

        var chatId = callbackQuery.Message.Chat.Id;
        var data = callbackQuery.Data ?? string.Empty;
        var parts = data.Split('|', 3, StringSplitOptions.TrimEntries);

        if (parts.Length != 3 || parts[0] != AiModelCallbackPrefix)
        {
            await _botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "Неизвестная кнопка",
                cancellationToken: stoppingToken);
            return;
        }

        try
        {
            var setting = await _imageSettings.SetActiveAsync(parts[1], parts[2], stoppingToken);

            await _botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                $"AI: {setting.Provider} / {setting.Model}",
                cancellationToken: stoppingToken);

            await _botClient.SendMessage(
                chatId,
                $"AI модель переключена:\nprovider: {setting.Provider}\nmodel: {setting.Model}",
                cancellationToken: stoppingToken);
        }
        catch (InvalidOperationException ex)
        {
            await _botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "Модель не переключена",
                cancellationToken: stoppingToken);

            await _botClient.SendMessage(chatId, ex.Message, cancellationToken: stoppingToken);
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
            "/newpack static ai Название пака | стиль — создать AI-стикер\n" +
            "/addsticker pack_name raw — добавить стикер в существующий пак\n" +
            "/aistatus — показать активный AI provider/model\n" +
            "/aimodels — показать доступные AI модели\n" +
            "/aimodel provider model — переключить AI модель\n" +
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

    private async Task SendAiStatusAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        var setting = await _imageSettings.GetActiveAsync(stoppingToken);

        await _botClient.SendMessage(
            context.ChatId,
            $"AI provider: {setting.Provider}\nAI model: {setting.Model}",
            cancellationToken: stoppingToken);
    }

    private async Task SendAiModelsAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        var buttons = ImageGenerationModelCatalog.ModelsByProvider
            .SelectMany(provider => provider.Value.Select(model => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{provider.Key}: {model}",
                    $"{AiModelCallbackPrefix}|{provider.Key}|{model}")
            }))
            .ToArray();

        await _botClient.SendMessage(
            context.ChatId,
            "Доступные AI модели:\n" +
            ImageGenerationModelCatalog.FormatSupportedModels() +
            "\n\nНажми на модель ниже или используй команду:\n/aimodel stability sd3.5-medium",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: stoppingToken);
    }

    private async Task HandleAiModelAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        var parts = context.Command.Arguments.Split(
            ' ',
            2,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            await _botClient.SendMessage(
                context.ChatId,
                "Формат: /aimodel provider model\nНапример: /aimodel stability sd3.5-medium",
                cancellationToken: stoppingToken);
            return;
        }

        try
        {
            var setting = await _imageSettings.SetActiveAsync(parts[0], parts[1], stoppingToken);

            await _botClient.SendMessage(
                context.ChatId,
                $"AI модель переключена:\nprovider: {setting.Provider}\nmodel: {setting.Model}",
                cancellationToken: stoppingToken);
        }
        catch (InvalidOperationException ex)
        {
            await _botClient.SendMessage(context.ChatId, ex.Message, cancellationToken: stoppingToken);
        }
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

        if (args.Style != "raw" && args.Style != "outline" && args.Style != "ai")
        {
            await _botClient.SendMessage(context.ChatId,
                "не известный параметр, доступные параметры: raw(исходное изображение без обработки)\n,outline(обводка)\n,ai(обработка ии , нужен промт после |)",
                cancellationToken: stoppingToken);
            return;
        }

        if (args.Style == "ai" && string.IsNullOrWhiteSpace(args.StylePrompt))
        {
            await _botClient.SendMessage(context.ChatId,
                "промт пустой\nФормат для AI:\n/newpack static ai Название пака | описание стилизации ",
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
        var finalFilePath = await _imageGeneration.PrepareStickerImageAsync(originalFilePath,draft.Id,args.Style,args.StylePrompt, stoppingToken);

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
        var finalFilePath = await _imageGeneration.PrepareStickerImageAsync(originalFilePath,draft.Id,args.Style,string.Empty, stoppingToken);


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


        await _draftService.MarkDraftCreatedAsync(draft.Id, stoppingToken);

        await _botClient.SendMessage(
            context.ChatId,
            $"link : {stickerLink}",
            cancellationToken: stoppingToken);

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
        var previewFilePath = await _imageProcess.CreatePreviewPngAsync(finalFilePath, stoppingToken);
        await using var previewStream = File.OpenRead(previewFilePath);

        await _botClient.SendPhoto(
            chatId,
            InputFile.FromStream(stream: previewStream, fileName: PreviewFileName),
            caption: "Preview 512x512",
            cancellationToken: stoppingToken);
    }

    private async Task SendUnknownAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        await _botClient.SendMessage(context.ChatId, "unknown", cancellationToken: stoppingToken);
    }
}
