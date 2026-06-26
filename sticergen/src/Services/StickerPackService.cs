using Microsoft.EntityFrameworkCore;
using sticergen.Data;
using sticergen.Data.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace sticergen.Services;

public class StickerPackService
{
    private const string StickerFileName = "sticker.webp";
    private const string DefaultStickerType = "static";

    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _db;
    private readonly PackNameGenerator _nameGen;

    public StickerPackService(AppDbContext db, ITelegramBotClient botClient, PackNameGenerator nameGen)
    {
        _db = db;
        _botClient = botClient;
        _nameGen = nameGen;
    }

    public async Task<string?> CreateStickerPackAsync(int draftId, CancellationToken cancellationToken)
    {
        var draft = await _db.Drafts
            .Include(x => x.Stickers)
            .FirstOrDefaultAsync(x => x.Id == draftId, cancellationToken);

        if (draft == null) return null;

        var stickers = draft.Stickers
            .OrderBy(x => x.SortOrder)
            .Where(x => !string.IsNullOrWhiteSpace(x.FinalFilePath))
            .ToList();

        if (stickers.Count == 0) return null;

        var bot = await _botClient.GetMe(cancellationToken);
        var botUsername = bot.Username
            ?? throw new InvalidOperationException("Telegram bot username is missing");
        var packName = _nameGen.Generate(draft.PackTitle, draft.UserId, draft.Id, botUsername);
        var packTitle = draft.PackTitle;

        var streams = new List<FileStream>();
        try
        {
            var inputStickers = stickers
                .Select(sticker =>
                {
                    var stream = File.OpenRead(sticker.FinalFilePath);
                    streams.Add(stream);

                    return new InputSticker(
                        InputFile.FromStream(stream, $"sticker-{sticker.Id}.webp"),
                        StickerFormat.Static,
                        new[] { sticker.Emoji });
                })
                .ToArray();

            await _botClient.CreateNewStickerSet(
                userId: draft.UserId,
                name: packName,
                title: packTitle,
                stickers: inputStickers,
                stickerType: StickerType.Regular,
                cancellationToken: cancellationToken);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }

        draft.PackName = packName;
        draft.Status = "created";

        await _db.SaveChangesAsync(cancellationToken);

        var stickerPack = new StickerPack
        {
            UserId = draft.UserId,
            ChatId = draft.ChatId,
            PackName = packName,
            PackTitle = packTitle,
            StickerType = DefaultStickerType,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.StickerPacks.AddAsync(stickerPack, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return $"https://t.me/addstickers/{packName}";
    }

    public async Task<List<StickerPack>> GetStickerPacksAsync(long userId, CancellationToken cancellationToken)
    {
        var userPacks = await _db.StickerPacks.Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        return userPacks;
    }

    public async Task<StickerPack?> ParseStickerPackAsync(
        string packName,
        long userId,
        CancellationToken cancellationToken)
    {
        var packIdentifier = packName.Trim();

        return await _db.StickerPacks
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(
                x => x.UserId == userId &&
                     (x.PackName == packIdentifier || x.PackTitle == packIdentifier),
                cancellationToken);
    }

    public async Task<string> AddStickerToPackAsync(
        string packName,
        long userId,
        string finalFilePath,
        string emoji,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(finalFilePath);

        var inputSticker = new InputSticker(
            InputFile.FromStream(stream, StickerFileName),
            StickerFormat.Static,
            new[] { emoji });

        await _botClient.AddStickerToSet(
            userId: userId,
            name: packName,
            sticker: inputSticker,
            cancellationToken: cancellationToken);

        return $"https://t.me/addstickers/{packName}";
    }

    public async Task<string> AddStickersToPackAsync(
        string packName,
        long userId,
        IReadOnlyList<string> finalFilePaths,
        string emoji,
        CancellationToken cancellationToken)
    {
        foreach (var finalFilePath in finalFilePaths)
        {
            await AddStickerToPackAsync(
                packName,
                userId,
                finalFilePath,
                emoji,
                cancellationToken);
        }

        return $"https://t.me/addstickers/{packName}";
    }
}
