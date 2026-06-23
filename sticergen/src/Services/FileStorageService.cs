
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

namespace sticergen.Services;

public class FileStorageService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IHostEnvironment _env;

    public  FileStorageService(ITelegramBotClient botClient, IHostEnvironment env)
    {
        _botClient = botClient;
        _env = env;
    }

    public async Task<string> SaveOriginalPhotoAsync(string fileId,int draftId,CancellationToken cancellationToken)
    {
        var tgFile = await _botClient.GetFile(fileId, cancellationToken);

        var rootPath = _env.ContentRootPath;

        var binIndex = rootPath.IndexOf(
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);

        if (binIndex >= 0)
        {
            rootPath = rootPath[..binIndex];
        }

        var directoryPath = Path.Combine(
            rootPath,
            "storage",
            "originals");

        Directory.CreateDirectory(directoryPath);

        var filePath = Path.Combine(
            directoryPath,
            $"{draftId}.png");

        await using var stream = File.Create(filePath);

        await _botClient.DownloadFile(tgFile, stream, cancellationToken);
        return filePath;
    }
}