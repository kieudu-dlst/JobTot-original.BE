using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using JobTot.Application.Profiles;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api.Controllers;

[
    ApiController,
    Route("api/employer/talent-discovery"),
    Authorize(Policy = EmployerAuthentication.Policy)
]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmployerTalentDiscoveryController(RecruitmentDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions ProfileJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<RecruiterProfile?> Member(CancellationToken ct) =>
        db
            .RecruiterProfileRecords.AsNoTracking()
            .Include(p => p.Company)
            .ThenInclude(c => c.HeadOfficeProvince)
            .Where(p =>
                p.AccountId == AccountId
                && p.Status == EntityStatus.Active
                && p.Account.Status == EntityStatus.Active
                && p.Company.Status == EntityStatus.Active
            )
            .OrderBy(p => p.JoinedAt)
            .ThenBy(p => p.Id)
            .FirstOrDefaultAsync(ct);

    // The candidate's existing "Tạm khóa" setting removes the profile from every discovery endpoint.
    private IQueryable<CandidateProfile> Discoverable() =>
        db.CandidateProfileRecords.Where(p =>
            p.Status == EntityStatus.Active
            && p.Account.Status == EntityStatus.Active
            && p.Account.AccountType == "Candidate"
            && p.Account.AccountStatus == "Active"
            && (p.ReadyStatus == "looking" || p.ReadyStatus == "considering")
        );

    private async Task<List<DiscoveryCampaign>> Campaigns(Guid companyId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(
            DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime
        );
        return await db
            .RecruitmentCampaignRecords.AsNoTracking()
            .Where(c =>
                c.CompanyId == companyId
                && c.Status == EntityStatus.Active
                && c.CampaignStatus == "Running"
                && (c.StartDate == null || c.StartDate <= today)
                && (c.EndDate == null || c.EndDate >= today)
            )
            .OrderBy(c => c.Title)
            .ThenBy(c => c.Id)
            .Select(c => new DiscoveryCampaign(
                c.Id,
                c.Title,
                c.Department,
                c.ProvinceId,
                c.Province.ProvinceName,
                c.WorkMode,
                c.ExperienceMinYears,
                c.CampaignSkills.Where(s =>
                        s.Status == EntityStatus.Active && s.Skill.Status == EntityStatus.Active
                    )
                    .Select(s => new DiscoveryCampaignSkill(s.Skill.SkillName, s.IsRequired))
                    .ToArray()
            ))
            .ToListAsync(ct);
    }

    private async Task<List<DiscoveryCandidate>> Candidates(
        CancellationToken ct,
        Guid? candidateId = null
    )
    {
        // Split skills from the profile row so large DetailsJson values are not repeated for each skill.
        var query = Discoverable().AsNoTracking().AsSplitQuery();
        if (candidateId.HasValue)
            query = query.Where(p => p.Id == candidateId);
        // Only these columns are read. Contact details and CV documents are never loaded by discovery.
        var rows = await query
            .Select(p => new
            {
                p.Id,
                p.AccountId,
                Name = p.Account.FullName,
                p.Headline,
                p.Summary,
                p.YearsExperience,
                p.ProvinceId,
                Location = p.Province == null ? null : p.Province.ProvinceName,
                p.ReadyStatus,
                p.ExpectedSalaryMin,
                p.ExpectedSalaryMax,
                p.DetailsJson,
                UpdatedAt = p.UpdatedDate ?? p.CreatedDate,
                Skills = p
                    .CandidateSkills.Where(s =>
                        s.Status == EntityStatus.Active && s.Skill.Status == EntityStatus.Active
                    )
                    .Select(s => s.Skill.SkillName)
                    .ToList(),
            })
            .ToListAsync(ct);
        return rows.Select(p =>
            {
                var document = ReadProfessionalDocument(p.DetailsJson);
                var skills = p
                    .Skills.Concat(document.Skills.Select(s => s.Title))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .GroupBy(TalentDiscoveryMatching.Normalize)
                    .Select(g => g.First().Trim())
                    .OrderBy(s => s)
                    .ToArray();
                return new DiscoveryCandidate(
                    p.Id,
                    p.AccountId,
                    p.UpdatedAt,
                    document.DesiredProvinceId,
                    new DiscoveryProfile(
                        p.Name,
                        p.Headline ?? document.Headline,
                        p.Summary ?? document.Summary,
                        document.AvatarUrl,
                        p.Location ?? document.Location,
                        p.ProvinceId ?? document.ProvinceId,
                        p.YearsExperience,
                        p.ReadyStatus!,
                        document.DesiredPosition,
                        document.WorkType,
                        document.DesiredLocation,
                        skills
                    ),
                    p.ExpectedSalaryMin ?? document.ExpectedSalaryMin,
                    p.ExpectedSalaryMax ?? document.ExpectedSalaryMax,
                    document.Experiences,
                    document.Education,
                    document.Certificates,
                    document.Projects
                );
            })
            .ToList();
    }

    private static CandidateProfileDocument ReadProfessionalDocument(string? json)
    {
        try
        {
            var document = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<CandidateProfileDocument>(json, ProfileJson);
            if (document is null)
                return new();
            document.Skills = (document.Skills ?? []).Where(s => s is not null).ToList();
            document.Experiences = (document.Experiences ?? []).Where(s => s is not null).ToList();
            document.Education = (document.Education ?? []).Where(s => s is not null).ToList();
            document.Certificates = (document.Certificates ?? [])
                .Where(s => s is not null)
                .ToList();
            document.Projects = (document.Projects ?? []).Where(s => s is not null).ToList();
            return document;
        }
        catch (JsonException)
        {
            return new();
        }
    }

    private async Task<CollectionState> Collections(Guid companyId, CancellationToken ct)
    {
        var interests = await db
            .EmployerCandidateInterests.AsNoTracking()
            .Where(i => i.CompanyId == companyId)
            .Select(i => i.CandidateId)
            .ToListAsync(ct);
        var talents = await db
            .TalentPoolEntryRecords.AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.Status == EntityStatus.Active)
            .OrderBy(t => t.AddedAt)
            .ThenBy(t => t.Id)
            .Select(t => new { t.CandidateId, t.Id })
            .ToListAsync(ct);
        return new(
            interests.ToHashSet(),
            talents.GroupBy(t => t.CandidateId).ToDictionary(g => g.Key, g => g.First().Id)
        );
    }

    private static DiscoveryCard Card(
        DiscoveryCandidate candidate,
        IReadOnlyList<TalentDiscoveryMatching.CampaignCriteria> campaigns,
        CollectionState collections
    )
    {
        var criteria = TalentDiscoveryMatching.PrepareCandidate(
            candidate.Profile.Headline,
            candidate.Profile.DesiredPosition,
            candidate.Profile.YearsExperience,
            candidate.Profile.ProvinceId,
            candidate.DesiredProvinceId,
            candidate.Profile.Skills
        );
        DiscoveryMatch? best = null;
        foreach (var campaign in campaigns)
        {
            var current = TalentDiscoveryMatching.Compare(criteria, campaign);
            var score = Nullable.Compare(current.Score, best?.Score);
            var title = Comparer<string?>.Default.Compare(
                current.CampaignTitle,
                best?.CampaignTitle
            );
            if (
                best is null
                || score > 0
                || score == 0
                    && (
                        title < 0
                        || title == 0 && Nullable.Compare(current.CampaignId, best.CampaignId) < 0
                    )
            )
                best = current;
        }
        var match = best ?? new DiscoveryMatch(null, null, null, [], [], []);
        var isSaved = collections.Saved.TryGetValue(candidate.Id, out var talentId);
        return new(
            candidate.Id,
            candidate.AccountId,
            candidate.Profile,
            match,
            collections.Interested.Contains(candidate.Id),
            isSaved,
            isSaved ? talentId : null
        );
    }

    private static object Stats(IReadOnlyList<DiscoveryCard> cards) =>
        new
        {
            discoverable = cards.Count,
            recommended = cards.Count(c => c.Match.Score >= 70),
            interested = cards.Count(c => c.IsInterested),
            saved = cards.Count(c => c.IsSaved),
        };

    [HttpGet("metadata")]
    public async Task<IActionResult> Metadata(CancellationToken ct)
    {
        if (ct.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        try
        {
            var member = await Member(ct);
            if (member is null)
                return NotFound();
            var campaigns = await Campaigns(member.CompanyId, ct);
            var preparedCampaigns = campaigns
                .Select(TalentDiscoveryMatching.PrepareCampaign)
                .ToArray();
            var candidates = await Candidates(ct);
            var collections = await Collections(member.CompanyId, ct);
            var cards = candidates.Select(p => Card(p, preparedCampaigns, collections)).ToList();
            var provinces = await db
                .ProvinceRecords.AsNoTracking()
                .Where(p => p.Status == EntityStatus.Active)
                .OrderBy(p => p.ProvinceName)
                .Select(p => new { p.Id, name = p.ProvinceName })
                .ToListAsync(ct);
            return Ok(
                new
                {
                    company = new
                    {
                        id = member.CompanyId,
                        name = member.Company.Name,
                        provinceId = member.Company.HeadOfficeProvinceId,
                        provinceName = member.Company.HeadOfficeProvince?.ProvinceName,
                    },
                    campaigns = campaigns.Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.Department,
                        c.ProvinceId,
                        province = c.Province,
                        c.ExperienceMinYears,
                        skills = c.Skills.Select(s => s.Name),
                    }),
                    provinces,
                    skills = cards
                        .SelectMany(c => c.Profile.Skills)
                        .GroupBy(TalentDiscoveryMatching.Normalize)
                        .Select(g => g.First())
                        .OrderBy(s => s),
                    stats = Stats(cards),
                    matchingExplanation = "Mức phù hợp so sánh kỹ năng, vị trí, kinh nghiệm và địa điểm với các chiến dịch đang chạy. Chỉ hiển thị hồ sơ đang tìm việc hoặc cân nhắc cơ hội.",
                }
            );
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, ct)
                || RequestCancellation.IsClientCancellation(exception, HttpContext.RequestAborted)
            )
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DiscoveryQuery query, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        try
        {
            if (query.MinYears > query.MaxYears)
                return BadRequest(
                    new ProblemDetails { Detail = "Khoảng kinh nghiệm không hợp lệ." }
                );
            var member = await Member(ct);
            if (member is null)
                return NotFound();
            var campaigns = await Campaigns(member.CompanyId, ct);
            if (query.CampaignId.HasValue && campaigns.All(c => c.Id != query.CampaignId))
                return NotFound();
            var contextCampaigns = query.CampaignId.HasValue
                ? campaigns.Where(c => c.Id == query.CampaignId).ToList()
                : campaigns;
            var preparedCampaigns = contextCampaigns
                .Select(TalentDiscoveryMatching.PrepareCampaign)
                .ToArray();
            var candidates = await Candidates(ct);
            var collections = await Collections(member.CompanyId, ct);
            var cards = candidates.Select(p => Card(p, preparedCampaigns, collections)).ToList();
            IEnumerable<DiscoveryCard> filtered = cards;
            if (query.ProvinceId.HasValue)
            {
                var preferredLocations = candidates.ToDictionary(
                    p => p.Id,
                    p => p.DesiredProvinceId
                );
                filtered = filtered.Where(c =>
                    c.Profile.ProvinceId == query.ProvinceId
                    || preferredLocations[c.CandidateId] == query.ProvinceId
                );
            }
            if (!string.IsNullOrEmpty(query.Readiness))
                filtered = filtered.Where(c => c.Profile.ReadyStatus == query.Readiness);
            if (query.MinYears.HasValue)
                filtered = filtered.Where(c => c.Profile.YearsExperience >= query.MinYears);
            if (query.MaxYears.HasValue)
                filtered = filtered.Where(c => c.Profile.YearsExperience <= query.MaxYears);
            if (query.MinScore.HasValue)
                filtered = filtered.Where(c => c.Match.Score >= query.MinScore);
            if (query.Collection == "interested")
                filtered = filtered.Where(c => c.IsInterested);
            if (query.Collection == "saved")
                filtered = filtered.Where(c => c.IsSaved);
            if (!string.IsNullOrWhiteSpace(query.Skill))
            {
                var skill = TalentDiscoveryMatching.Normalize(query.Skill);
                filtered = filtered.Where(c =>
                    c.Profile.Skills.Any(s => TalentDiscoveryMatching.Normalize(s) == skill)
                );
            }
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keywords = TalentDiscoveryMatching
                    .Normalize(query.Keyword)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                filtered = filtered.Where(c =>
                {
                    var text = TalentDiscoveryMatching.Normalize(
                        string.Join(
                            " ",
                            new[]
                            {
                                c.Profile.Name,
                                c.Profile.Headline,
                                c.Profile.DesiredPosition,
                                c.Profile.Summary,
                            }.Concat(c.Profile.Skills)
                        )
                    );
                    return keywords.All(term => text.Contains(term, StringComparison.Ordinal));
                });
            }
            var matches = filtered.ToList();
            var total = matches.Count;
            var updatedAt = candidates.ToDictionary(p => p.Id, p => p.UpdatedAt);
            var ordered =
                query.Sort == "newest"
                    ? matches
                        .OrderByDescending(c => updatedAt[c.CandidateId])
                        .ThenBy(c => c.CandidateId)
                : query.Sort == "experience"
                    ? matches
                        .OrderByDescending(c => c.Profile.YearsExperience)
                        .ThenByDescending(c => c.Match.Score)
                        .ThenBy(c => c.CandidateId)
                : matches
                    .OrderByDescending(c => c.Match.Score)
                    .ThenByDescending(c => updatedAt[c.CandidateId])
                    .ThenBy(c => c.CandidateId);
            return Ok(
                new
                {
                    items = ordered
                        .Skip((query.Page - 1) * query.PageSize)
                        .Take(query.PageSize)
                        .ToList(),
                    totalCount = total,
                    query.Page,
                    query.PageSize,
                    stats = Stats(cards),
                    context = new
                    {
                        title = query.CampaignId.HasValue
                            ? contextCampaigns[0].Title
                            : member.Company.Name,
                        description = contextCampaigns.Count == 0
                            ? "Chưa có chiến dịch đang chạy để tính mức phù hợp. Bạn vẫn có thể tìm kiếm và lưu hồ sơ."
                        : query.CampaignId.HasValue
                            ? "So sánh với kỹ năng, vị trí, kinh nghiệm và địa điểm của chiến dịch đã chọn."
                        : "Mỗi hồ sơ được so sánh với các chiến dịch đang chạy của công ty và hiển thị chiến dịch phù hợp nhất.",
                    },
                }
            );
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, ct)
                || RequestCancellation.IsClientCancellation(exception, HttpContext.RequestAborted)
            )
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        [FromQuery] Guid? campaignId,
        CancellationToken ct
    )
    {
        if (ct.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        try
        {
            var member = await Member(ct);
            if (member is null)
                return NotFound();
            var campaigns = await Campaigns(member.CompanyId, ct);
            if (campaignId.HasValue && campaigns.All(c => c.Id != campaignId))
                return NotFound();
            var candidate = (await Candidates(ct, id)).SingleOrDefault();
            if (candidate is null)
                return NotFound();
            var preparedCampaigns = (
                campaignId.HasValue ? campaigns.Where(c => c.Id == campaignId) : campaigns
            )
                .Select(TalentDiscoveryMatching.PrepareCampaign)
                .ToArray();
            var card = Card(candidate, preparedCampaigns, await Collections(member.CompanyId, ct));
            return Ok(
                new
                {
                    card.CandidateId,
                    card.AccountId,
                    profile = new
                    {
                        card.Profile.Name,
                        card.Profile.Headline,
                        card.Profile.Summary,
                        card.Profile.AvatarUrl,
                        card.Profile.Location,
                        card.Profile.ProvinceId,
                        card.Profile.YearsExperience,
                        card.Profile.ReadyStatus,
                        card.Profile.DesiredPosition,
                        card.Profile.WorkType,
                        card.Profile.DesiredLocation,
                        card.Profile.Skills,
                        candidate.ExpectedSalaryMin,
                        candidate.ExpectedSalaryMax,
                        candidate.Experiences,
                        candidate.Education,
                        candidate.Certificates,
                        candidate.Projects,
                    },
                    card.Match,
                    card.IsInterested,
                    card.IsSaved,
                    card.TalentId,
                }
            );
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, ct)
                || RequestCancellation.IsClientCancellation(exception, HttpContext.RequestAborted)
            )
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    [HttpPut("{id:guid}/interest")]
    public async Task<IActionResult> Interest(Guid id, InterestRequest input, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        try
        {
            var member = await Member(ct);
            if (member is null || !await Discoverable().AnyAsync(p => p.Id == id, ct))
                return NotFound();
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var tx = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                        : null;
                    var interest = await db.EmployerCandidateInterests.SingleOrDefaultAsync(
                        i => i.CompanyId == member.CompanyId && i.CandidateId == id,
                        ct
                    );
                    if (input.Interested && interest is null)
                        db.EmployerCandidateInterests.Add(
                            new EmployerCandidateInterest
                            {
                                CompanyId = member.CompanyId,
                                CandidateId = id,
                                InterestedByRecruiterId = member.Id,
                            }
                        );
                    else if (!input.Interested && interest is not null)
                        db.EmployerCandidateInterests.Remove(interest);
                    await db.SaveChangesAsync(ct);
                    var talentId = await db
                        .TalentPoolEntryRecords.AsNoTracking()
                        .Where(t =>
                            t.CompanyId == member.CompanyId
                            && t.CandidateId == id
                            && t.Status == EntityStatus.Active
                        )
                        .OrderBy(t => t.AddedAt)
                        .ThenBy(t => t.Id)
                        .Select(t => (Guid?)t.Id)
                        .FirstOrDefaultAsync(ct);
                    if (tx is not null)
                        await tx.CommitAsync(ct);
                    return (IActionResult)Ok(
                        new
                        {
                            isInterested = input.Interested,
                            isSaved = talentId.HasValue,
                            talentId,
                        }
                    );
                });
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, ct)
                || RequestCancellation.IsClientCancellation(exception, HttpContext.RequestAborted)
            )
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    [HttpPut("{id:guid}/saved")]
    public async Task<IActionResult> Saved(Guid id, SavedRequest input, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        try
        {
            var member = await Member(ct);
            if (member is null || !await Discoverable().AnyAsync(p => p.Id == id, ct))
                return NotFound();
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var tx = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                        : null;
                    var entries = await db
                        .TalentPoolEntryRecords.Where(t =>
                            t.CompanyId == member.CompanyId && t.CandidateId == id
                        )
                        .OrderBy(t => t.AddedAt)
                        .ThenBy(t => t.Id)
                        .ToListAsync(ct);
                    var talent = entries.FirstOrDefault(t => t.Status == EntityStatus.Active);
                    if (input.Saved && talent is null)
                    {
                        talent = entries.FirstOrDefault();
                        if (talent is null)
                        {
                            talent = new TalentPoolEntry
                            {
                                CompanyId = member.CompanyId,
                                CandidateId = id,
                                AddedByRecruiterId = member.Id,
                                AddedAt = DateTimeOffset.UtcNow,
                                ReadinessStatus = "unknown",
                                CreatedAccountId = AccountId,
                            };
                            db.TalentPoolEntryRecords.Add(talent);
                        }
                        else
                        {
                            talent.Status = EntityStatus.Active;
                            talent.AddedAt = DateTimeOffset.UtcNow;
                            talent.AddedByRecruiterId = member.Id;
                            talent.UpdatedDate = DateTimeOffset.UtcNow;
                            talent.UpdatedAccountId = AccountId;
                        }
                    }
                    if (!input.Saved)
                        foreach (var entry in entries.Where(t => t.Status == EntityStatus.Active))
                        {
                            entry.Status = EntityStatus.Inactive;
                            entry.UpdatedDate = DateTimeOffset.UtcNow;
                            entry.UpdatedAccountId = AccountId;
                        }
                    await db.SaveChangesAsync(ct);
                    var isInterested = await db
                        .EmployerCandidateInterests.AsNoTracking()
                        .AnyAsync(i => i.CompanyId == member.CompanyId && i.CandidateId == id, ct);
                    if (tx is not null)
                        await tx.CommitAsync(ct);
                    return (IActionResult)Ok(
                        new
                        {
                            isInterested,
                            isSaved = input.Saved,
                            talentId = input.Saved ? talent?.Id : (Guid?)null,
                        }
                    );
                });
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, ct)
                || RequestCancellation.IsClientCancellation(exception, HttpContext.RequestAborted)
            )
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    public sealed class DiscoveryQuery
    {
        [StringLength(200)]
        public string? Keyword { get; set; }
        public Guid? CampaignId { get; set; }
        public Guid? ProvinceId { get; set; }

        [StringLength(200)]
        public string? Skill { get; set; }

        [Range(0, 80)]
        public int? MinYears { get; set; }

        [Range(0, 80)]
        public int? MaxYears { get; set; }

        [Range(0, 100)]
        public int? MinScore { get; set; }

        [RegularExpression("^(looking|considering)$")]
        public string? Readiness { get; set; }

        [RegularExpression("^(all|interested|saved)$")]
        public string Collection { get; set; } = "all";

        [RegularExpression("^(match|newest|experience)$")]
        public string Sort { get; set; } = "match";

        [Range(1, 100000)]
        public int Page { get; set; } = 1;

        [Range(1, 50)]
        public int PageSize { get; set; } = 9;
    }

    public sealed record InterestRequest(bool Interested);

    public sealed record SavedRequest(bool Saved);

    private sealed record CollectionState(HashSet<Guid> Interested, Dictionary<Guid, Guid> Saved);

    private sealed record DiscoveryCandidate(
        Guid Id,
        Guid AccountId,
        DateTimeOffset UpdatedAt,
        Guid? DesiredProvinceId,
        DiscoveryProfile Profile,
        decimal? ExpectedSalaryMin,
        decimal? ExpectedSalaryMax,
        IReadOnlyList<ProfileEntry> Experiences,
        IReadOnlyList<ProfileEntry> Education,
        IReadOnlyList<ProfileEntry> Certificates,
        IReadOnlyList<ProfileEntry> Projects
    );

    private sealed record DiscoveryProfile(
        string Name,
        string? Headline,
        string? Summary,
        string? AvatarUrl,
        string? Location,
        Guid? ProvinceId,
        int? YearsExperience,
        string ReadyStatus,
        string? DesiredPosition,
        string? WorkType,
        string? DesiredLocation,
        IReadOnlyList<string> Skills
    );

    private sealed record DiscoveryCard(
        Guid CandidateId,
        Guid AccountId,
        DiscoveryProfile Profile,
        DiscoveryMatch Match,
        bool IsInterested,
        bool IsSaved,
        Guid? TalentId
    );
}
