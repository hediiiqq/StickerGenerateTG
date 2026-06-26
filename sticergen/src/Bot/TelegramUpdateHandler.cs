using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using sticergen.Bot.Commands;
using sticergen.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace sticergen.Bot;

public class TelegramUpdateHandler
{
    private static readonly TimeSpan MediaGroupCollectDelay = TimeSpan.FromMilliseconds(1200);
    private static readonly TimeSpan MediaGroupCacheTtl = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, MediaGroupBuffer> _mediaGroups = new();
    private readonly ConcurrentDictionary<string, CachedMediaGroup> _completedMediaGroups = new();
    private readonly ConcurrentDictionary<string, string> _messageMediaGroupKeys = new();
    private readonly CommandParser _parser;
    private readonly IServiceScopeFactory _serviceProvider;

    public TelegramUpdateHandler(CommandParser parser, IServiceScopeFactory serviceProvider)
    {
        _parser = parser;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery?.From is not null)
        {
            using var callbackScope = _serviceProvider.CreateScope();
            var callbackHandler = callbackScope.ServiceProvider.GetRequiredService<CommandHandler>();
            await callbackHandler.HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
            return;
        }

        var message = update.Message;
        if (message?.From is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(message.MediaGroupId) && GetOwnPhotoFileId(message) is not null)
        {
            HandleMediaGroupMessage(message, cancellationToken);
            return;
        }

        var commandText = message.Text ?? message.Caption;
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return;
        }

        await HandleCommandMessageAsync(
            message.From.Id,
            message.Chat.Id,
            commandText,
            GetPhotoFileIds(message),
            cancellationToken);
    }

    private async Task HandleCommandMessageAsync(
        long userId,
        long chatId,
        string commandText,
        List<string> photoFileIds,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();
        var command = _parser.Parse(commandText);

        var context = new BotCommandContext
        {
            UserId = userId,
            ChatId = chatId,
            Command = command,
            PhotoFileId = photoFileIds.FirstOrDefault(),
            PhotoFileIds = photoFileIds,
        };

        context.HasPhoto = context.PhotoFileIds.Count > 0;

        await handler.HandleAsync(context, cancellationToken);
    }

    private void HandleMediaGroupMessage(Message message, CancellationToken cancellationToken)
    {
        if (message.From is null ||
            string.IsNullOrWhiteSpace(message.MediaGroupId) ||
            GetOwnPhotoFileId(message) is not { } photoFileId)
        {
            return;
        }

        var key = $"{message.Chat.Id}:{message.MediaGroupId}";
        var buffer = _mediaGroups.GetOrAdd(
            key,
            _ => new MediaGroupBuffer(message.From.Id, message.Chat.Id));

        int version;
        lock (buffer.SyncRoot)
        {
            if (!string.IsNullOrWhiteSpace(message.Caption))
            {
                buffer.CommandText = message.Caption;
            }

            buffer.Photos.Add(new MediaGroupPhoto(message.MessageId, photoFileId));
            version = ++buffer.Version;
        }

        _ = ProcessMediaGroupAfterDelayAsync(key, version, cancellationToken);
    }

    private async Task ProcessMediaGroupAfterDelayAsync(
        string key,
        int version,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(MediaGroupCollectDelay, cancellationToken);

            if (!_mediaGroups.TryGetValue(key, out var buffer))
            {
                return;
            }

            string? commandText;
            List<string> photoFileIds;

            lock (buffer.SyncRoot)
            {
                if (buffer.Version != version)
                {
                    return;
                }

                commandText = buffer.CommandText;
                photoFileIds = buffer.Photos
                    .OrderBy(x => x.MessageId)
                    .Select(x => x.FileId)
                    .ToList();
            }

            _mediaGroups.TryRemove(key, out _);

            StoreCompletedMediaGroup(key, buffer, photoFileIds);

            if (string.IsNullOrWhiteSpace(commandText) || photoFileIds.Count == 0)
            {
                return;
            }

            await HandleCommandMessageAsync(
                buffer.UserId,
                buffer.ChatId,
                commandText,
                photoFileIds,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }

    private List<string> GetPhotoFileIds(Message message)
    {
        if (message.ReplyToMessage is not null)
        {
            var mediaGroupPhotoFileIds = GetMediaGroupPhotoFileIds(message.ReplyToMessage);
            if (mediaGroupPhotoFileIds.Count > 0)
            {
                return mediaGroupPhotoFileIds;
            }
        }

        var photoFileId = GetPhotoFileId(message);
        return photoFileId is null ? new List<string>() : new List<string> { photoFileId };
    }

    private List<string> GetMediaGroupPhotoFileIds(Message message)
    {
        PurgeExpiredMediaGroups();

        var groupKey = GetMediaGroupKey(message)
            ?? (_messageMediaGroupKeys.TryGetValue(GetMessageKey(message.Chat.Id, message.MessageId), out var cachedKey)
                ? cachedKey
                : null);

        if (groupKey is null)
        {
            return new List<string>();
        }

        if (_completedMediaGroups.TryGetValue(groupKey, out var cachedGroup) &&
            cachedGroup.ExpiresAt > DateTime.UtcNow)
        {
            return cachedGroup.PhotoFileIds;
        }

        if (_mediaGroups.TryGetValue(groupKey, out var activeGroup))
        {
            lock (activeGroup.SyncRoot)
            {
                return activeGroup.Photos
                    .OrderBy(x => x.MessageId)
                    .Select(x => x.FileId)
                    .ToList();
            }
        }

        return new List<string>();
    }

    private static string? GetPhotoFileId(Message message)
    {
        var photos = message.Photo ?? message.ReplyToMessage?.Photo;

        return photos?.LastOrDefault()?.FileId;
    }

    private static string? GetOwnPhotoFileId(Message message)
    {
        return message.Photo?.LastOrDefault()?.FileId;
    }

    private void StoreCompletedMediaGroup(
        string key,
        MediaGroupBuffer buffer,
        List<string> photoFileIds)
    {
        PurgeExpiredMediaGroups();

        var expiresAt = DateTime.UtcNow.Add(MediaGroupCacheTtl);
        _completedMediaGroups[key] = new CachedMediaGroup(photoFileIds, expiresAt);

        foreach (var photo in buffer.Photos)
        {
            _messageMediaGroupKeys[GetMessageKey(buffer.ChatId, photo.MessageId)] = key;
        }
    }

    private void PurgeExpiredMediaGroups()
    {
        var now = DateTime.UtcNow;
        var expiredGroupKeys = _completedMediaGroups
            .Where(x => x.Value.ExpiresAt <= now)
            .Select(x => x.Key)
            .ToHashSet();

        if (expiredGroupKeys.Count == 0)
        {
            return;
        }

        foreach (var groupKey in expiredGroupKeys)
        {
            _completedMediaGroups.TryRemove(groupKey, out _);
        }

        foreach (var item in _messageMediaGroupKeys)
        {
            if (expiredGroupKeys.Contains(item.Value))
            {
                _messageMediaGroupKeys.TryRemove(item.Key, out _);
            }
        }
    }

    private static string? GetMediaGroupKey(Message message)
    {
        return string.IsNullOrWhiteSpace(message.MediaGroupId)
            ? null
            : $"{message.Chat.Id}:{message.MediaGroupId}";
    }

    private static string GetMessageKey(long chatId, int messageId)
    {
        return $"{chatId}:{messageId}";
    }

    private sealed class MediaGroupBuffer
    {
        public MediaGroupBuffer(long userId, long chatId)
        {
            UserId = userId;
            ChatId = chatId;
        }

        public object SyncRoot { get; } = new();
        public long UserId { get; }
        public long ChatId { get; }
        public string? CommandText { get; set; }
        public List<MediaGroupPhoto> Photos { get; } = new();
        public int Version { get; set; }
    }

    private sealed record MediaGroupPhoto(int MessageId, string FileId);

    private sealed record CachedMediaGroup(List<string> PhotoFileIds, DateTime ExpiresAt);
}
