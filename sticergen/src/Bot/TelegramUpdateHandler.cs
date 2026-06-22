using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;


namespace sticergen.Bot;

public class TelegramUpdateHandler
{
    private readonly ITelegramBotClient _bot;

    public TelegramUpdateHandler(ITelegramBotClient bot)
    {
        _bot = bot;
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

        if (upt.Message.Text == "/start")
        {
            var chatId = upt.Message.Chat.Id;
            await _bot.SendMessage(chatId, "hello");
        }

        if (upt.Message.Text == "/help")
        {
            var chatId = upt.Message.Chat.Id;
            await _bot.SendMessage(chatId, "help");
        }
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