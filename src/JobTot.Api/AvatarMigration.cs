using System.Text.Json;
using JobTot.Application.Profiles;
using JobTot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api;

public static class AvatarMigration
{
    public static async Task<int> RunAsync(
        RecruitmentDbContext db,
        AvatarFileStorage storage,
        CancellationToken ct
    )
    {
        var ids = await db
            .CandidateProfileRecords.AsNoTracking()
            .Where(p => p.DetailsJson != null && p.DetailsJson.Contains("data:image/"))
            .Select(p => p.Id)
            .ToListAsync(ct);
        var migrated = 0;
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            var profile = await db.CandidateProfileRecords.SingleAsync(p => p.Id == id, ct);
            var document = JsonSerializer.Deserialize<CandidateProfileDocument>(
                profile.DetailsJson!
            );
            var avatar = document?.AvatarUrl;
            var png =
                avatar?.StartsWith("data:image/png;base64,", StringComparison.Ordinal) == true;
            if (
                !png
                && avatar?.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal) != true
            )
            {
                db.ChangeTracker.Clear();
                continue;
            }
            var content = Convert.FromBase64String(avatar![(avatar!.IndexOf(',') + 1)..]);
            document!.AvatarUrl = await storage.SaveAsync(content, png, ct);
            profile.DetailsJson = JsonSerializer.Serialize(document);
            await db.SaveChangesAsync(ct);
            migrated++;
            db.ChangeTracker.Clear();
        }
        return migrated;
    }
}
