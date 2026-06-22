using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

var token = builder.Configuration["Telegram:BotToken"];

if (string.IsNullOrWhiteSpace(token))
{
    throw new InvalidOperationException("Telegram bot token is missing");
}

builder.Services.AddSingleton<ITelegramBotClient>(
    new TelegramBotClient(token));

using var host = builder.Build();

var botClient = host.Services.GetRequiredService<ITelegramBotClient>();
var bot = await botClient.GetMe();

Console.WriteLine($"Подключён бот: @{bot.Username}");