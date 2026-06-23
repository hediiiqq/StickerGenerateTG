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

    public async Task<Draft> CreateNewPackDraftAsync(long userId, long chatId, string? photoFileId,
        NewPackCommandArgs args,
        CancellationToken cancellationToken)
    {
        var draft = new Draft();
        draft.UserId = userId;
        draft.ChatId = chatId;
        draft.Mode = "newpack";
        draft.PackTitle = args.PackTitle;
        draft.StickerType = args.StickerType;
        draft.Style = args.Style;
        draft.Status = "pending";
        draft.CreatedAt = DateTime.UtcNow;
        _db.Drafts.Add(draft);
        if (photoFileId != null)
        {
            var draftstiker = new DraftSticker();
            draftstiker.TelegramFileId = photoFileId;
            draftstiker.SortOrder = 0;
            draft.Stickers.Add(draftstiker);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task<List<Draft>> GetUserDraftsAsync(long userId, CancellationToken cancellationToken)
    {
        var drafts = await _db.Drafts.Where(x => x.UserId == userId)
            .Include(x => x.Stickers)
            .ToListAsync(cancellationToken);
        return drafts;
    }
}