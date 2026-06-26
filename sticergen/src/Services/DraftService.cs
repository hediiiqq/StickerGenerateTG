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
        IReadOnlyList<string> photoFileIds,
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

        for (var i = 0; i < photoFileIds.Count; i++)
        {
            draft.Stickers.Add(new DraftSticker
            {
                TelegramFileId = photoFileIds[i],
                SortOrder = i,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task<DraftSticker?> UpdateDraftStickerFilePathsAsync(
        int draftStickerId,
        string originalFile,
        string finalFile,
        CancellationToken cancellationToken)
    {
        var sticker = await _db.DraftStickers.FirstOrDefaultAsync(x => x.Id == draftStickerId, cancellationToken);
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

    public async Task<Draft?> GetUserDraftAsync(
        int draftId,
        long userId,
        CancellationToken cancellationToken)
    {
        return await _db.Drafts
            .Include(x => x.Stickers)
            .FirstOrDefaultAsync(
                x => x.Id == draftId && x.UserId == userId,
                cancellationToken);
    }

    public async Task<Draft> CreateAddStickerDraftAsync(
        long userId,
        long chatId,
        IReadOnlyList<string> photoFileIds,
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

        for (var i = 0; i < photoFileIds.Count; i++)
        {
            draft.Stickers.Add(new DraftSticker
            {
                TelegramFileId = photoFileIds[i],
                SortOrder = i,
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

    public async Task<bool> MarkDraftDeletedAsync(
        int draftId,
        long userId,
        CancellationToken cancellationToken)
    {
        var draft = await _db.Drafts
            .FirstOrDefaultAsync(
                x => x.Id == draftId && x.UserId == userId,
                cancellationToken);

        if (draft is null)
        {
            return false;
        }

        draft.Status = "deleted";

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
