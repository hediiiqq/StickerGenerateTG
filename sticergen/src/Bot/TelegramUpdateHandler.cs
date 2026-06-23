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

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update upt, CancellationToken cancellationToken)
    {
        var message = upt.Message;
        if (message == null) return;

        string commandText;

        if (message.Text != null)
        {
            commandText = message.Text;
        }

        else if (message.Caption != null)
        {
            commandText = message.Caption;
        }
        else
        {
            return;
        }

        PhotoSize[]? photos = null;
        if (message.Photo != null)
        {
            photos = message.Photo;
        }
        else if (message.ReplyToMessage?.Photo != null)
        {
            photos = message.ReplyToMessage.Photo;
        }

        var photoFileId = photos?.Last().FileId;


        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();


        var command = _parser.Parse(commandText);


        if (message.From != null)
        {
            var context = new BotCommandContext
            {
                UserId = message.From.Id,
                ChatId = message.Chat.Id,
                Command = command,
                PhotoFileId = photoFileId,
                HasPhoto = photoFileId != null,
            };

            await handler.HandleAsync(context, cancellationToken);
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
}