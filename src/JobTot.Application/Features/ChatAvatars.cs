using System.Text.Json;
using JobTot.Domain.Enums;
namespace JobTot.Application.Features;

internal static class ChatAvatars
{
    public static async Task<Dictionary<Guid, string?>> ReadAsync(
        IRecruitmentData db,
        IEnumerable<Guid> accountIds,
        CancellationToken ct
    )
    {
        var ids = accountIds.Distinct().ToArray();
        var urls = new Dictionary<Guid, string?>();
        if (ids.Length == 0)
            return urls;
        var candidates = await db
            .CandidateProfileRecords.ReadOnly()
            .Where(p =>
                ids.Contains(p.AccountId)
                && p.Status == EntityStatus.Active
                && p.Account.AccountType == "Candidate"
            )
            .Select(p => new { p.AccountId, p.DetailsJson })
            .ToListAsync(ct);
        foreach (var candidate in candidates)
            urls[candidate.AccountId] = CandidateAvatar(candidate.DetailsJson);

        var recruiters = await db
            .RecruiterProfileRecords.ReadOnly()
            .Where(p =>
                ids.Contains(p.AccountId)
                && p.Account.AccountType == "Employer"
                && p.Status == EntityStatus.Active
                && p.Company.Status == EntityStatus.Active
            )
            .GroupBy(p => p.AccountId)
            .Select(group =>
                group
                    .OrderBy(p => p.JoinedAt)
                    .ThenBy(p => p.Id)
                    .Select(p => new
                    {
                        p.AccountId,
                        p.Company.Culture,
                        LogoUrl = p.Company.LogoFile == null ? null : p.Company.LogoFile.Url,
                    })
                    .First()
            )
            .ToListAsync(ct);
        foreach (var recruiter in recruiters)
            if (!urls.ContainsKey(recruiter.AccountId))
                urls[recruiter.AccountId] = EmployerCompanyService
                    .ReadBrand(recruiter.Culture, recruiter.LogoUrl, null)
                    .LogoUrl;
        return urls;
    }

    private static string? CandidateAvatar(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return null;
        try
        {
            using var document = JsonDocument.Parse(details);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            foreach (var property in document.RootElement.EnumerateObject())
                if (
                    property.Name.Equals("avatarUrl", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String
                )
                    return property.Value.GetString();
        }
        catch (JsonException)
        { /* Legacy invalid profile data must not prevent messaging. */
        }
        return null;
    }
}
