using sticergen.Bot.Models;
using sticergen.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace sticergen.Bot.Commands;

public class CommandHandler
{
    private const string AiModelCallbackPrefix = "aimodel";
    private const string AiDraftCallbackPrefix = "aidraft";
    private const string AiDraftCreateAction = "create";
    private const string AiDraftDeleteAction = "delete";
    private const string AiDraftRegenerateAction = "regen";
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
        var parts = data.Split('|', StringSplitOptions.TrimEntries);

        if (parts.Length == 3 && parts[0] == AiModelCallbackPrefix)
        {
            await HandleAiModelCallbackAsync(callbackQuery, chatId, parts, stoppingToken);
            return;
        }

        if (parts.Length >= 3 && parts[0] == AiDraftCallbackPrefix)
        {
            await HandleAiDraftCallbackAsync(callbackQuery, chatId, parts, stoppingToken);
            return;
        }

        await _botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            "Неизвестная кнопка",
            cancellationToken: stoppingToken);
    }

    private async Task HandleAiModelCallbackAsync(
        CallbackQuery callbackQuery,
        long chatId,
        string[] parts,
        CancellationToken stoppingToken)
    {
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

    private async Task HandleAiDraftCallbackAsync(
        CallbackQuery callbackQuery,
        long chatId,
        string[] parts,
        CancellationToken stoppingToken)
    {
        if (!int.TryParse(parts[2], out var draftId))
        {
            await _botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "Черновик не найден",
                cancellationToken: stoppingToken);
            return;
        }

        var action = parts[1];

        try
        {
            switch (action)
            {
                case AiDraftCreateAction:
                    await HandleAiDraftCreateAsync(callbackQuery, chatId, draftId, stoppingToken);
                    break;

                case AiDraftDeleteAction:
                    await HandleAiDraftDeleteAsync(callbackQuery, chatId, draftId, stoppingToken);
                    break;

                case AiDraftRegenerateAction:
                    await HandleAiDraftRegenerateAsync(callbackQuery, chatId, parts, draftId, stoppingToken);
                    break;

                default:
                    await _botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Неизвестная кнопка",
                        cancellationToken: stoppingToken);
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            await _botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "Действие не выполнено",
                cancellationToken: stoppingToken);

            await _botClient.SendMessage(chatId, ex.Message, cancellationToken: stoppingToken);
        }
    }

    private async Task HandleAiDraftCreateAsync(
        CallbackQuery callbackQuery,
        long chatId,
        int draftId,
        CancellationToken stoppingToken)
    {
        var draft = await GetPendingAiDraftAsync(draftId, callbackQuery.From.Id, stoppingToken);
        var stickerLink = await _stickerPack.CreateStickerPackAsync(draft.Id, stoppingToken)
            ?? throw new InvalidOperationException("Не удалось создать пак: нет готовых стикеров.");

        await _botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            "Пак создан",
            cancellationToken: stoppingToken);

        await RemoveInlineKeyboardAsync(callbackQuery, stoppingToken);

        await _botClient.SendMessage(
            chatId,
            $"Пак создан\n" +
            $"Название: {draft.PackTitle}\n" +
            $"Стикеров: {draft.Stickers.Count}\n" +
            stickerLink,
            cancellationToken: stoppingToken);
    }

    private async Task HandleAiDraftDeleteAsync(
        CallbackQuery callbackQuery,
        long chatId,
        int draftId,
        CancellationToken stoppingToken)
    {
        var draft = await GetPendingAiDraftAsync(draftId, callbackQuery.From.Id, stoppingToken);
        await _draftService.MarkDraftDeletedAsync(draft.Id, callbackQuery.From.Id, stoppingToken);

        await _botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            "Черновик удалён",
            cancellationToken: stoppingToken);

        await RemoveInlineKeyboardAsync(callbackQuery, stoppingToken);

        await _botClient.SendMessage(
            chatId,
            $"AI-пак удалён\nНазвание: {draft.PackTitle}",
            cancellationToken: stoppingToken);
    }

    private async Task HandleAiDraftRegenerateAsync(
        CallbackQuery callbackQuery,
        long chatId,
        string[] parts,
        int draftId,
        CancellationToken stoppingToken)
    {
        if (parts.Length != 4 || !int.TryParse(parts[3], out var stickerId))
        {
            await _botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "Стикер не найден",
                cancellationToken: stoppingToken);
            return;
        }

        var draft = await GetPendingAiDraftAsync(draftId, callbackQuery.From.Id, stoppingToken);
        var stickers = draft.Stickers
            .OrderBy(x => x.SortOrder)
            .ToList();
        var sticker = stickers.FirstOrDefault(x => x.Id == stickerId)
            ?? throw new InvalidOperationException("Стикер не найден в этом паке.");
        var stickerIndex = stickers.FindIndex(x => x.Id == sticker.Id) + 1;

        await _botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            $"Перегенерация фото {stickerIndex}",
            cancellationToken: stoppingToken);

        await _botClient.SendMessage(
            chatId,
            $"AI перегенерация фото {stickerIndex}/{stickers.Count}",
            cancellationToken: stoppingToken);

        var originalFilePath = sticker.OriginalFilePath;
        if (string.IsNullOrWhiteSpace(originalFilePath) || !File.Exists(originalFilePath))
        {
            originalFilePath = await _fileService.SaveOriginalPhotoAsync(
                sticker.TelegramFileId,
                draft.Id,
                sticker.Id,
                stoppingToken);
        }

        var finalFilePath = await _imageGeneration.PrepareStickerImageAsync(
            originalFilePath,
            draft.Id,
            sticker.Id,
            draft.Style,
            draft.StylePrompt,
            stoppingToken);

        await _draftService.UpdateDraftStickerFilePathsAsync(
            sticker.Id,
            originalFilePath,
            finalFilePath,
            stoppingToken);

        var refreshedDraft = await GetPendingAiDraftAsync(draft.Id, callbackQuery.From.Id, stoppingToken);
        var finalFilePaths = refreshedDraft.Stickers
            .OrderBy(x => x.SortOrder)
            .Select(x => x.FinalFilePath)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        await SendStickerPreviewsAsync(
            chatId,
            finalFilePaths,
            $"Обновлённое превью: {finalFilePaths.Count}",
            stoppingToken);

        await RemoveInlineKeyboardAsync(callbackQuery, stoppingToken);
        await SendAiDraftControlsAsync(chatId, refreshedDraft, stoppingToken);
    }

    private async Task<Data.Models.Draft> GetPendingAiDraftAsync(
        int draftId,
        long userId,
        CancellationToken stoppingToken)
    {
        var draft = await _draftService.GetUserDraftAsync(draftId, userId, stoppingToken)
            ?? throw new InvalidOperationException("AI-пак не найден.");

        if (draft.Mode != "newpack" || draft.Style != "ai")
        {
            throw new InvalidOperationException("Это действие доступно только для AI-паков.");
        }

        if (draft.Status != "pending")
        {
            throw new InvalidOperationException("Этот AI-пак уже создан или удалён.");
        }

        return draft;
    }

    private async Task RemoveInlineKeyboardAsync(
        CallbackQuery callbackQuery,
        CancellationToken stoppingToken)
    {
        if (callbackQuery.Message is null)
        {
            return;
        }

        await _botClient.EditMessageReplyMarkup(
            callbackQuery.Message.Chat.Id,
            callbackQuery.Message.MessageId,
            replyMarkup: null,
            cancellationToken: stoppingToken);
    }

    private async Task SendStartAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        await _botClient.SendMessage(context.ChatId, "Эта команда предназначена для запуска бота", cancellationToken: stoppingToken);
    }

    private async Task SendHelpAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        await _botClient.SendMessage(
            context.ChatId,
            "/start — запустить бота\n" +
            "/help — показать справку по командам\n" +
            "/newpack static raw Название пака — создать новый пак из одного или нескольких фото\n" +
            "/newpack static ai Название пака | стиль — создать AI-стикер пак\n" +
            "/addsticker Название пака raw — добавить один или несколько стикеров в существующий пак\n" +
            "/aistatus — показать активный AI provider/model\n" +
            "/aimodels — показать доступные AI модели\n" +
            "/aimodel provider model — переключить AI модель\n" +
            "/mypacks — показать мои черновики\n\n" +
            "Фото можно отправить с подписью-командой, альбомом с подписью-командой или написать команду ответом на фото.",
            cancellationToken: stoppingToken);
    }

    private async Task SendMyPacksAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        var packs = await _stickerPack.GetStickerPacksAsync(context.UserId, stoppingToken);

        if (packs.Count == 0)
        {
            await _botClient.SendMessage(
                context.ChatId,
                "У тебя пока нет созданных паков.",
                cancellationToken: stoppingToken);

            return;
        }

        var packLines = packs.Select(pack =>
            $"{pack.PackTitle} - {pack.StickerType}\n" +
            $"/addsticker {pack.PackTitle} raw\n" +
            $"https://t.me/addstickers/{pack.PackName}");

        var message = "Твои паки:\n" + string.Join("\n\n", packLines);

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
        var photoFileIds = context.PhotoFileIds;

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

        if (!context.HasPhoto || photoFileIds.Count == 0)
        {
            await _botClient.SendMessage(
                context.ChatId,
                "Прикрепи одно или несколько фото к команде или отправь команду ответом на фото.",
                cancellationToken: stoppingToken);

            return;
        }

        if (args.Style != "raw" && args.Style != "ai")
        {
            await _botClient.SendMessage(context.ChatId,
                "не известный параметр, доступные параметры: raw(исходное изображение без обработки)\n,ai(обработка ии , нужен промт после |)",
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
            photoFileIds,
            args,
            stoppingToken);

        var finalFilePaths = await PrepareDraftStickersAsync(
            draft,
            args.Style,
            args.StylePrompt,
            context.ChatId,
            stoppingToken);

        await SendStickerPreviewsAsync(
            context.ChatId,
            finalFilePaths,
            $"Превью готовых стикеров: {finalFilePaths.Count}",
            stoppingToken);

        if (args.Style == "ai")
        {
            await SendAiDraftControlsAsync(context.ChatId, draft, stoppingToken);
            return;
        }

        var stickerLink = await _stickerPack.CreateStickerPackAsync(draft.Id, stoppingToken);

        await _botClient.SendMessage(
            context.ChatId,
            $"Пак создан\n" +
            $"Название: {args.PackTitle}\n" +
            $"Стикеров: {finalFilePaths.Count}\n" +
            stickerLink,
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
                "Формат: /addsticker Название пака raw",
                cancellationToken: stoppingToken);

            return;
        }

        if (args.Style != "raw")
        {
            await _botClient.SendMessage(
                context.ChatId,
                "Для добавления стикера сейчас доступен только стиль raw.\nФормат: /addsticker Название пака raw",
                cancellationToken: stoppingToken);

            return;
        }

        var pack = await _stickerPack.ParseStickerPackAsync(args.PackName, context.UserId, stoppingToken);

        if (pack is null)
        {
            await _botClient.SendMessage(
                context.ChatId,
                "Пак не найден. Используй название из /mypacks, например:\n/addsticker Название пака raw",
                cancellationToken: stoppingToken);
            return;
        }

        var photoFileIds = context.PhotoFileIds;

        if (!context.HasPhoto || photoFileIds.Count == 0)
        {
            await _botClient.SendMessage(context.ChatId, "Прикрепи одно или несколько фото", cancellationToken: stoppingToken);
            return;
        }

        var draft = await _draftService.CreateAddStickerDraftAsync(
            context.UserId,
            context.ChatId,
            photoFileIds,
            args,
            pack,
            stoppingToken);

        var finalFilePaths = await PrepareDraftStickersAsync(
            draft,
            args.Style,
            string.Empty,
            context.ChatId,
            stoppingToken);

        await SendStickerPreviewsAsync(
            context.ChatId,
            finalFilePaths,
            $"Превью новых стикеров: {finalFilePaths.Count}",
            stoppingToken);

        var stickerLink = await _stickerPack.AddStickersToPackAsync(
            pack.PackName,
            context.UserId,
            finalFilePaths,
            DefaultStickerEmoji,
            stoppingToken);


        await _draftService.MarkDraftCreatedAsync(draft.Id, stoppingToken);

        await _botClient.SendMessage(
            context.ChatId,
            $"Стикеры добавлены\n" +
            $"Пак: {pack.PackTitle}\n" +
            $"Добавлено: {finalFilePaths.Count}\n" +
            stickerLink,
            cancellationToken: stoppingToken);

    }

    private async Task<List<string>> PrepareDraftStickersAsync(
        Data.Models.Draft draft,
        string style,
        string stylePrompt,
        long chatId,
        CancellationToken stoppingToken)
    {
        var finalFilePaths = new List<string>();
        var stickers = draft.Stickers
            .OrderBy(x => x.SortOrder)
            .ToList();
        var isAiStyle = string.Equals(style, "ai", StringComparison.OrdinalIgnoreCase);

        for (var index = 0; index < stickers.Count; index++)
        {
            var sticker = stickers[index];
            var originalFilePath = await _fileService.SaveOriginalPhotoAsync(
                sticker.TelegramFileId,
                draft.Id,
                sticker.Id,
                stoppingToken);

            if (isAiStyle)
            {
                await _botClient.SendMessage(
                    chatId,
                    $"AI обработка фото {index + 1}/{stickers.Count}",
                    cancellationToken: stoppingToken);
            }

            var finalFilePath = await _imageGeneration.PrepareStickerImageAsync(
                originalFilePath,
                draft.Id,
                sticker.Id,
                style,
                stylePrompt,
                stoppingToken);

            await _draftService.UpdateDraftStickerFilePathsAsync(
                sticker.Id,
                originalFilePath,
                finalFilePath,
                stoppingToken);

            finalFilePaths.Add(finalFilePath);
        }

        return finalFilePaths;
    }

    private async Task SendStickerPreviewsAsync(
        long chatId,
        IReadOnlyList<string> finalFilePaths,
        string caption,
        CancellationToken stoppingToken)
    {
        if (finalFilePaths.Count == 0)
        {
            return;
        }

        if (finalFilePaths.Count == 1)
        {
            var previewFilePath = await _imageProcess.CreatePreviewPngAsync(finalFilePaths[0], stoppingToken);
            await using var previewStream = File.OpenRead(previewFilePath);

            await _botClient.SendPhoto(
                chatId,
                InputFile.FromStream(stream: previewStream, fileName: PreviewFileName),
                caption: caption,
                cancellationToken: stoppingToken);

            return;
        }

        foreach (var chunk in finalFilePaths.Chunk(10))
        {
            var streams = new List<FileStream>();

            try
            {
                var media = new List<IAlbumInputMedia>();

                for (var i = 0; i < chunk.Length; i++)
                {
                    var previewFilePath = await _imageProcess.CreatePreviewPngAsync(chunk[i], stoppingToken);
                    var stream = File.OpenRead(previewFilePath);
                    streams.Add(stream);

                    var photo = new InputMediaPhoto(
                        InputFile.FromStream(stream, $"preview-{i + 1}.png"));

                    if (i == 0)
                    {
                        photo.Caption = caption;
                    }

                    media.Add(photo);
                }

                await _botClient.SendMediaGroup(
                    chatId,
                    media,
                    cancellationToken: stoppingToken);
            }
            finally
            {
                foreach (var stream in streams)
                {
                    await stream.DisposeAsync();
                }
            }
        }
    }

    private async Task SendAiDraftControlsAsync(
        long chatId,
        Data.Models.Draft draft,
        CancellationToken stoppingToken)
    {
        await _botClient.SendMessage(
            chatId,
            $"AI-пак готов к проверке\n" +
            $"Название: {draft.PackTitle}\n" +
            $"Стикеров: {draft.Stickers.Count}\n\n" +
            "Создай пак, удали черновик или перегенерируй отдельное фото.",
            replyMarkup: BuildAiDraftKeyboard(draft),
            cancellationToken: stoppingToken);
    }

    private static InlineKeyboardMarkup BuildAiDraftKeyboard(Data.Models.Draft draft)
    {
        var rows = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "Создать пак",
                    $"{AiDraftCallbackPrefix}|{AiDraftCreateAction}|{draft.Id}"),
                InlineKeyboardButton.WithCallbackData(
                    "Удалить пак",
                    $"{AiDraftCallbackPrefix}|{AiDraftDeleteAction}|{draft.Id}")
            }
        };

        var regenerateButtons = draft.Stickers
            .OrderBy(x => x.SortOrder)
            .Select((sticker, index) => InlineKeyboardButton.WithCallbackData(
                $"Перегенерировать {index + 1}",
                $"{AiDraftCallbackPrefix}|{AiDraftRegenerateAction}|{draft.Id}|{sticker.Id}"));

        rows.AddRange(regenerateButtons.Chunk(2).Select(x => x.ToArray()));

        return new InlineKeyboardMarkup(rows);
    }

    private async Task SendUnknownAsync(BotCommandContext context, CancellationToken stoppingToken)
    {
        await _botClient.SendMessage(context.ChatId, "unknown", cancellationToken: stoppingToken);
    }
}
