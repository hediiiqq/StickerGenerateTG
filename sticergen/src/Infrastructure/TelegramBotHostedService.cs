using Microsoft.Extensions.Hosting;
using sticergen.Bot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace sticergen.Infrastructure;

public class TelegramBotHostedService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramUpdateHandler _updateHandler;

    public TelegramBotHostedService(ITelegramBotClient botClient, TelegramUpdateHandler updateHandler)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bot = await _botClient.GetMe();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        _botClient.StartReceiving(
            _updateHandler.HandleUpdateAsync,
            _updateHandler.HandleErrorAsync,
            receiverOptions,
            stoppingToken);
        Console.WriteLine($"Подключён бот: @{bot.Username}");
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}