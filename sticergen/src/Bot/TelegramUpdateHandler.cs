using Microsoft.Extensions.DependencyInjection;
using sticergen.Bot.Commands;
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
        if (upt.Message == null) return;

        var commandText = string.Empty;

        if (upt.Message.Text != null)
        {
            commandText = upt.Message.Text;
        }

        else if (upt.Message.Caption != null)
        {
            commandText = upt.Message.Caption;
        }
        else
        {
            return;
        }

        var message = upt.Message;

        var currentMessageHasPhoto = message.Photo != null && message.Photo.Length > 0;
        var replyMessageHasPhoto = message.ReplyToMessage?.Photo != null && message.ReplyToMessage.Photo.Length > 0;

        var hasPhoto = currentMessageHasPhoto || replyMessageHasPhoto;
        Console.WriteLine($"Has photo: {hasPhoto}");
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();


        var command = _parser.Parse(commandText);
        if (upt.Message.From != null)
            await handler.HandleAsync(upt.Message.From.Id, upt.Message.Chat.Id, command, cancellationToken);
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }
}