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
        if (upt.Message == null)
        {
            return;
        }

        if (upt.Message.Text == null)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();
        var message = upt.Message.Text;

        var command = _parser.Parse(upt.Message.Text);
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