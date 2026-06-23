using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using sticergen.Bot;
using sticergen.Bot.Commands;
using sticergen.Configuration;
using sticergen.Infrastructure;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((context, config) =>
{
    config.SetBasePath(Directory.GetCurrentDirectory());
    config.AddUserSecrets(typeof(Program).Assembly);
}).ConfigureServices((context, services) =>
{
    var configuration = context.Configuration;
    services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));

    services.AddHostedService<TelegramBotHostedService>();

    services.AddSingleton<ITelegramBotClient>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
        if (string.IsNullOrEmpty(options.BotToken))
        {
            throw new InvalidOperationException("Telegram bot token is missing");
        }

        return new TelegramBotClient(options.BotToken);
    });
    services.AddSingleton<TelegramUpdateHandler>();
    services.AddSingleton<CommandParser>();
    services.AddSingleton<CommandHandler>();
    services.AddSingleton<ArgumentsParser>();

}).Build();
await host.RunAsync();