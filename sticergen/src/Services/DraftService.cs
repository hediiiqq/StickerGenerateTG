using Microsoft.EntityFrameworkCore;
using sticergen.Bot.Models;
using sticergen.Data;
using sticergen.Data.Models;

namespace sticergen.Services;

public class DraftService
{
    private readonly AppDbContext _db;

    public DraftService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Draft> CreateNewPackDraftAsync(
        long userId,
        long chatId,
        string? photoFileId,
        NewPackCommandArgs args,
        CancellationToken cancellationToken)
    {
        var draft = new Draft
        {
            UserId = userId,
            ChatId = chatId,
            Mode = "newpack",
            PackTitle = args.PackTitle,
            StickerType = args.StickerType,
            Style = args.Style,
            StylePrompt = args.StylePrompt,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
        };

        _db.Drafts.Add(draft);

        if (photoFileId != null)
        {
            draft.Stickers.Add(new DraftSticker
            {
                TelegramFileId = photoFileId,
                SortOrder = 0,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task<DraftSticker?> UpdateDraftStickerFilePathsAsync(
        int draftId,
        string originalFile,
        string finalFile,
        CancellationToken cancellationToken)
    {
        var sticker = await _db.DraftStickers.FirstOrDefaultAsync(x => x.DraftId == draftId, cancellationToken);
        if (sticker == null) return null;

        sticker.OriginalFilePath = originalFile;
        sticker.FinalFilePath = finalFile;

        await _db.SaveChangesAsync(cancellationToken);
        return sticker;
    }

    public async Task<List<Draft>> GetUserDraftsAsync(long userId, CancellationToken cancellationToken)
    {
        var drafts = await _db.Drafts.Where(x => x.UserId == userId)
            .Include(x => x.Stickers)
            .ToListAsync(cancellationToken);
        return drafts;
    }

    public async Task<Draft> CreateAddStickerDraftAsync(
        long userId,
        long chatId,
        string? photoFileId,
        AddStickerCommandArgs args,
        StickerPack pack,
        CancellationToken cancellationToken)
    {
        var draft = new Draft
        {
            UserId = userId,
            ChatId = chatId,
            Mode = "addsticker",
            PackName = pack.PackName,
            PackTitle = pack.PackTitle,
            StickerType = pack.StickerType,
            Style = args.Style,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
        };

        _db.Drafts.Add(draft);

        if (photoFileId != null)
        {
            draft.Stickers.Add(new DraftSticker
            {
                TelegramFileId = photoFileId,
                SortOrder = 0,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return draft;
    }
    public async Task<bool> MarkDraftCreatedAsync(
        int draftId,
        CancellationToken cancellationToken)
    {
        var draft = await _db.Drafts
            .FirstOrDefaultAsync(x => x.Id == draftId, cancellationToken);

        if (draft is null)
        {
            return false;
        }

        draft.Status = "created";

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
