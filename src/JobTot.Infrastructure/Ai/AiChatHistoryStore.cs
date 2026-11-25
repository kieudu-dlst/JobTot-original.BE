using JobTot.Application.Ai;
using JobTot.Domain.Entities;
using JobTot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace JobTot.Infrastructure.Ai;

public sealed class AiChatHistoryStore(RecruitmentDbContext db, IHostEnvironment environment) : IAiChatHistoryStore
{
    private string ImageRoot => Path.Combine(environment.ContentRootPath, "App_Data", "ai-chat");
    public async Task<IReadOnlyList<AiConversationSummary>> ListAsync(Guid accountId, CancellationToken ct)
    {
        var groups = await db.AiChatMessages.AsNoTracking().Where(m => m.AccountId == accountId)
            .GroupBy(m => m.ConversationId).Select(g => new
            {
                Id = g.Key, UpdatedAt = g.Max(m => m.UpdatedAt),
                Title = g.Where(m => m.Role == "user").OrderBy(m => m.Id).Select(m => m.Content).FirstOrDefault() ?? "Cuộc trò chuyện",
            }).OrderByDescending(g => g.UpdatedAt).ToListAsync(ct);
        return groups.Select(g => new AiConversationSummary(g.Id, g.Title.Length > 80 ? g.Title[..80] + "…" : g.Title, g.UpdatedAt)).ToArray();
    }
    public async Task<AiHistoryPage?> ReadAsync(Guid accountId, Guid conversationId, long? beforeId, CancellationToken ct)
    {
        var query = db.AiChatMessages.AsNoTracking().Where(m => m.AccountId == accountId && m.ConversationId == conversationId);
        if (!await query.AnyAsync(ct)) return null;
        if (beforeId.HasValue) query = query.Where(m => m.Id < beforeId.Value);
        var rows = await query.OrderByDescending(m => m.Id).Take(101)
            .Select(m => new AiHistoryMessage(m.Id, m.Role, m.Content, m.State, m.ImageName != null, m.CreatedAt)).ToListAsync(ct);
        return new(rows.Take(100).Reverse().ToArray(), rows.Count > 100);
    }
    public async Task<AiHistoryImage?> ImageAsync(Guid accountId, Guid conversationId, long messageId, CancellationToken ct)
    {
        var row = await db.AiChatMessages.AsNoTracking().FirstOrDefaultAsync(m => m.AccountId == accountId
            && m.ConversationId == conversationId && m.Id == messageId, ct);
        if (row?.ImageName is null || row.ImageMimeType is null) return null;
        var file = Path.Combine(ImageRoot, Path.GetFileName(row.ImageName));
        return File.Exists(file) ? new(await File.ReadAllBytesAsync(file, ct), row.ImageMimeType) : null;
    }
    public async Task<AiTurn> BeginAsync(Guid accountId, Guid? conversationId, string question, CareerChatImage? image, CancellationToken ct)
    {
        if (conversationId.HasValue && !await db.AiChatMessages.AnyAsync(m => m.AccountId == accountId && m.ConversationId == conversationId.Value, ct))
            throw new CareerSearchException("ai_conversation_not_found", "Không tìm thấy cuộc trò chuyện.");
        var id = conversationId ?? Guid.NewGuid();
        string? imageName = null;
        if (image is not null)
        {
            CareerImageValidation.Validate(image);
            imageName = Guid.NewGuid().ToString("N") + (image.MimeType == "image/png" ? ".png" : ".jpg");
            Directory.CreateDirectory(ImageRoot);
            try { await File.WriteAllBytesAsync(Path.Combine(ImageRoot, imageName), Convert.FromBase64String(image.Data), ct); }
            catch { if (File.Exists(Path.Combine(ImageRoot, imageName))) File.Delete(Path.Combine(ImageRoot, imageName)); throw; }
        }
        var now = DateTimeOffset.UtcNow;
        var turnId = Guid.NewGuid();
        var user = new AiChatMessage { AccountId = accountId, ConversationId = id, TurnId = turnId,
            Role = "user", Content = question, State = "Completed", CreatedAt = now, UpdatedAt = now,
            ImageName = imageName, ImageMimeType = image?.MimeType };
        var assistant = new AiChatMessage { AccountId = accountId, ConversationId = id, TurnId = turnId,
            Role = "assistant", State = "Pending", CreatedAt = now.AddTicks(1), UpdatedAt = now.AddTicks(1) };
        db.AiChatMessages.AddRange(user, assistant);
        try { await db.SaveChangesAsync(ct); }
        catch { if (imageName is not null) File.Delete(Path.Combine(ImageRoot, imageName)); throw; }
        return new(id, assistant.Id);
    }
    public async Task FinishAsync(Guid accountId, AiTurn turn, string content, string state, CancellationToken ct)
    {
        var row = await db.AiChatMessages.SingleOrDefaultAsync(m => m.AccountId == accountId && m.ConversationId == turn.ConversationId
            && m.Id == turn.AssistantMessageId && m.Role == "assistant", ct);
        if (row is null) return; // Deleted during generation: never recreate its messages.
        row.Content = content;
        row.State = state;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            if (await db.AiChatMessages.AsNoTracking().AnyAsync(m => m.Id == row.Id, ct)) throw;
            db.Entry(row).State = EntityState.Detached;
        }
    }
}
