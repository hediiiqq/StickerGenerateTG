using Microsoft.EntityFrameworkCore;
using sticergen.Data;
using sticergen.Data.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace sticergen.Services;

public class StickerPackService
{
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


        var packName = _nameGen.Generate(draft.PackTitle, draft.UserId, draft.Id, bot.Username);


        var packTitle = draft.PackTitle;

        await using var stream = File.OpenRead(firstSticker.FinalFilePath);

        var inputSticker = new InputSticker(
            InputFile.FromStream(stream, "sticker.png"),
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

        var stickerPack = new StickerPack();

        stickerPack.UserId = draft.UserId;
        stickerPack.ChatId = draft.ChatId;
        stickerPack.PackName = packName;
        stickerPack.PackTitle = packTitle;
        stickerPack.StickerType = "static";
        stickerPack.CreatedAt = DateTime.UtcNow;
        await _db.StickerPacks.AddAsync(stickerPack, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);


        return $"https://t.me/addstickers/{packName}";
    }
}