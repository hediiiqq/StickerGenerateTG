using sticergen.Bot.Commands;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;


namespace sticergen.Bot;

public class TelegramUpdateHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly CommandParser _parser;
    private readonly CommandHandler _handler;

    public TelegramUpdateHandler(ITelegramBotClient bot, CommandParser parser, CommandHandler handler)
    {
        _bot = bot;
        _parser = parser;
        _handler = handler;
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

        var message = upt.Message.Text;

        var command = _parser.Parse(upt.Message.Text);
        await _handler.HandleAsync(upt.Message.Chat.Id, command, cancellationToken);
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