using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using sticergen.Bot;
using sticergen.Bot.Commands;
using sticergen.Configuration;
using sticergen.Data;
using sticergen.Infrastructure;
using sticergen.Services;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((context, config) =>
{
    config.SetBasePath(Directory.GetCurrentDirectory());
    config.AddUserSecrets(typeof(Program).Assembly);
}).ConfigureServices((context, services) =>
{
    var configuration = context.Configuration;
    services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));

    services.Configure<ImageGenerationOptions>(configuration.GetSection("ImageGeneration"));

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("DB connection string is missing");
    }

    services.AddHostedService<TelegramBotHostedService>();
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

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
    services.AddSingleton<ArgumentsParser>();
    services.AddSingleton<PackNameGenerator>();

    services.AddScoped<CommandHandler>();
    services.AddScoped<DraftService>();
    services.AddScoped<FileStorageService>();
    services.AddScoped<ImageProcessingService>();
    services.AddScoped<StickerPackService>();
    services.AddHttpClient<CloudflareImageProvider>();
    services.AddHttpClient<StabilityImageProvider>();
    services.AddScoped<IImageGenerationProvider>(sp => sp.GetRequiredService<StabilityImageProvider>());
    services.AddScoped<IImageGenerationProvider>(sp => sp.GetRequiredService<CloudflareImageProvider>());
    services.AddScoped<ImageGenerationSettingsService>();
    services.AddScoped<ImageGenerationService>();

}).Build();
await host.RunAsync();
