using Microsoft.Extensions.DependencyInjection;
using sticergen.Bot.Commands;
using sticergen.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace sticergen.Bot;

public class TelegramUpdateHandler
{
    private readonly CommandParser _parser;
    private readonly IServiceScopeFactory _serviceProvider;

    public TelegramUpdateHandler(CommandParser parser, IServiceScopeFactory serviceProvider)
    {
        _parser = parser;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message?.From is null)
        {
            return;
        }

        var commandText = message.Text ?? message.Caption;
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();
        var command = _parser.Parse(commandText);

        var context = new BotCommandContext
        {
            UserId = message.From.Id,
            ChatId = message.Chat.Id,
            Command = command,
            PhotoFileId = GetPhotoFileId(message),
        };

        context.HasPhoto = context.PhotoFileId is not null;

        await handler.HandleAsync(context, cancellationToken);
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

    private static string? GetPhotoFileId(Message message)
    {
        var photos = message.Photo ?? message.ReplyToMessage?.Photo;

        return photos?.LastOrDefault()?.FileId;
    }
}
