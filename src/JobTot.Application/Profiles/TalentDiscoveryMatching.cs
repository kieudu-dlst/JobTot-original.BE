using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JobTot.Application.Profiles;

public sealed record DiscoveryMatch(
    int? Score,
    Guid? CampaignId,
    string? CampaignTitle,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills
);

public sealed record DiscoveryCampaign(
    Guid Id,
    string Title,
    string? Department,
    Guid ProvinceId,
    string? Province,
    string? WorkMode,
    int? ExperienceMinYears,
    IReadOnlyList<DiscoveryCampaignSkill> Skills
);

public sealed record DiscoveryCampaignSkill(string Name, bool Required);

/// <summary>Transparent, deterministic comparison of professional fields. No personal demographics are used.</summary>
public static class TalentDiscoveryMatching
{
    public sealed record CandidateCriteria(
        int? YearsExperience,
        Guid? ProvinceId,
        Guid? DesiredProvinceId,
        IReadOnlySet<string> Skills,
        IReadOnlySet<string> Role
    );

    public sealed record CampaignSkillCriteria(string Name, bool Required, string NormalizedName);

    public sealed record CampaignCriteria(
        DiscoveryCampaign Campaign,
        IReadOnlyList<CampaignSkillCriteria> Skills,
        IReadOnlySet<string> Role,
        int SkillWeight
    );

    private static readonly HashSet<string> GenericRoleWords = new(StringComparer.Ordinal)
    {
        "nhan",
        "vien",
        "chuyen",
        "tuyen",
        "dung",
        "senior",
        "junior",
        "fresher",
        "intern",
        "staff",
        "the",
        "and",
        "of",
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var builder = new StringBuilder();
        foreach (var c in value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(c == 'đ' ? 'd' : c);
        }
        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ");
    }

    public static DiscoveryMatch Compare(
        string? headline,
        string? desiredPosition,
        int? yearsExperience,
        Guid? provinceId,
        Guid? desiredProvinceId,
        IReadOnlyList<string> skills,
        DiscoveryCampaign campaign
    ) =>
        Compare(
            PrepareCandidate(
                headline,
                desiredPosition,
                yearsExperience,
                provinceId,
                desiredProvinceId,
                skills
            ),
            PrepareCampaign(campaign)
        );

    public static CandidateCriteria PrepareCandidate(
        string? headline,
        string? desiredPosition,
        int? yearsExperience,
        Guid? provinceId,
        Guid? desiredProvinceId,
        IReadOnlyList<string> skills
    ) =>
        new(
            yearsExperience,
            provinceId,
            desiredProvinceId,
            skills.Select(Normalize).ToHashSet(StringComparer.Ordinal),
            RoleTokens($"{headline} {desiredPosition}")
        );

    public static CampaignCriteria PrepareCampaign(DiscoveryCampaign campaign)
    {
        var campaignSkills = campaign
            .Skills.Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .GroupBy(s => Normalize(s.Name))
            .Select(g => new CampaignSkillCriteria(g.First().Name, g.Any(s => s.Required), g.Key))
            .ToArray();
        return new(
            campaign,
            campaignSkills,
            RoleTokens(campaign.Title),
            campaignSkills.Sum(s => s.Required ? 2 : 1)
        );
    }

    public static DiscoveryMatch Compare(
        CandidateCriteria candidate,
        CampaignCriteria preparedCampaign
    )
    {
        var campaign = preparedCampaign.Campaign;
        var campaignSkills = preparedCampaign.Skills;
        var reasons = new List<string>();
        var matched = campaignSkills
            .Where(s => candidate.Skills.Contains(s.NormalizedName))
            .ToArray();
        var missing = campaignSkills
            .Where(s => !candidate.Skills.Contains(s.NormalizedName))
            .Select(s => s.Name)
            .ToArray();
        double points = 0;
        double available = 25; // A campaign title always contributes role criteria.

        if (campaignSkills.Count > 0)
        {
            available += 55;
            points += 55d * matched.Sum(s => s.Required ? 2 : 1) / preparedCampaign.SkillWeight;
            if (matched.Length > 0)
                reasons.Add(
                    $"Khớp {matched.Length}/{campaignSkills.Count} kỹ năng: {string.Join(", ", matched.Select(s => s.Name))}"
                );
        }

        var campaignRole = preparedCampaign.Role;
        var candidateRole = candidate.Role;
        var roleOverlap =
            campaignRole.Count == 0
                ? 0
                : (double)campaignRole.Count(candidateRole.Contains) / campaignRole.Count;
        points += 25 * roleOverlap;
        if (roleOverlap >= .5)
            reasons.Add("Định hướng nghề nghiệp phù hợp với vị trí tuyển dụng");

        if (campaign.ExperienceMinYears is > 0)
        {
            available += 10;
            if (candidate.YearsExperience.HasValue)
            {
                points +=
                    10
                    * Math.Clamp(
                        (double)candidate.YearsExperience.Value / campaign.ExperienceMinYears.Value,
                        0,
                        1
                    );
                if (candidate.YearsExperience >= campaign.ExperienceMinYears)
                    reasons.Add($"Đáp ứng kinh nghiệm tối thiểu {campaign.ExperienceMinYears} năm");
            }
        }

        available += 10;
        if (campaign.WorkMode == "remote")
        {
            // Remote work does not require residence in the employer's province.
            points += 10;
            reasons.Add("Chiến dịch cho phép làm việc từ xa");
        }
        else if (
            candidate.ProvinceId == campaign.ProvinceId
            || candidate.DesiredProvinceId == campaign.ProvinceId
        )
        {
            points += 10;
            reasons.Add("Địa điểm làm việc phù hợp");
        }

        return new(
            (int)Math.Round(points * 100 / available, MidpointRounding.AwayFromZero),
            campaign.Id,
            campaign.Title,
            reasons,
            matched.Select(s => s.Name).ToArray(),
            missing
        );
    }

    private static HashSet<string> RoleTokens(string? value) =>
        Regex
            .Split(Normalize(value), @"[^\p{L}\p{N}+#.]+")
            .Where(s => s.Length > 1 && !GenericRoleWords.Contains(s))
            .ToHashSet(StringComparer.Ordinal);
}
