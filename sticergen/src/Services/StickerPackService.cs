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

        var firstSticker = draft.Stickers
            .OrderBy(x => x.SortOrder)
            .FirstOrDefault();

        if (firstSticker == null) return null;

        var bot = await _botClient.GetMe(cancellationToken);
        var botUsername = bot.Username
            ?? throw new InvalidOperationException("Telegram bot username is missing");
        var packName = _nameGen.Generate(draft.PackTitle, draft.UserId, draft.Id, botUsername);
        var packTitle = draft.PackTitle;

        await using var stream = File.OpenRead(firstSticker.FinalFilePath);

        var inputSticker = new InputSticker(
            InputFile.FromStream(stream, StickerFileName),
            StickerFormat.Static,
            new[] { firstSticker.Emoji });

        await _botClient.CreateNewStickerSet(
            userId: draft.UserId,
            name: packName,
            title: packTitle,
            stickers: new[] { inputSticker },
            stickerType: StickerType.Regular,
            cancellationToken: cancellationToken);

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
        return await _db.StickerPacks.FirstOrDefaultAsync(
            x => x.PackName == packName && x.UserId == userId,
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
}
