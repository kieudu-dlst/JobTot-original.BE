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

[ApiController, Route("api/employer/candidates"), Authorize(Policy = EmployerAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmployerCandidatesController(RecruitmentDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions ProfileJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private sealed record RecruiterMembership(Guid Id, Guid CompanyId);

    private async Task<RecruiterMembership?> Member(CancellationToken ct)
    {
        try
        {
            return await db
                .RecruiterProfileRecords.Where(p =>
                    p.AccountId == AccountId
                    && p.Status == EntityStatus.Active
                    && p.Company.Status == EntityStatus.Active
                )
                .OrderBy(p => p.JoinedAt)
                .ThenBy(p => p.Id)
                .Select(p => new RecruiterMembership(p.Id, p.CompanyId))
                .FirstOrDefaultAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Member"
            );
            throw;
        }
    }

    private IQueryable<JobApplication> Applications(Guid company)
    {
        try
        {
            return db.JobApplicationRecords.Where(a =>
                a.Campaign.CompanyId == company
                && a.Status == EntityStatus.Active
                && a.Candidate.Status == EntityStatus.Active
                && a.Candidate.Account.Status == EntityStatus.Active
                && (
                    a.Source != "Talent Pool"
                    || (
                        a.Candidate.Account.AccountStatus == "Active"
                        && (
                            a.Candidate.ReadyStatus == "looking"
                            || a.Candidate.ReadyStatus == "considering"
                        )
                    )
                )
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Applications"
            );
            throw;
        }
    }

    private IQueryable<TalentPoolEntry> Talents(Guid company)
    {
        try
        {
            return db.TalentPoolEntryRecords.Where(t =>
                t.CompanyId == company
                && t.Status == EntityStatus.Active
                && t.Candidate.Status == EntityStatus.Active
                && t.Candidate.Account.Status == EntityStatus.Active
                && (
                    (
                        t.Candidate.Account.AccountStatus == "Active"
                        && (
                            t.Candidate.ReadyStatus == "looking"
                            || t.Candidate.ReadyStatus == "considering"
                        )
                    )
                    || db.JobApplicationRecords.Any(a =>
                        a.CandidateId == t.CandidateId
                        && a.Campaign.CompanyId == company
                        && a.Status == EntityStatus.Active
                        && a.Source != "Talent Pool"
                    )
                    || db.CandidateJobSubmissions.Any(s =>
                        s.CandidateId == t.CandidateId && s.Job.CompanyId == company
                    )
                )
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Talents"
            );
            throw;
        }
    }

    private IQueryable<Interview> Interviews(Guid company)
    {
        try
        {
            return db.InterviewRecords.Where(i =>
                i.Application.Campaign.CompanyId == company && i.Status == EntityStatus.Active
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Interviews"
            );
            throw;
        }
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> Metadata(CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var campaigns = await db
                .RecruitmentCampaignRecords.Where(c =>
                    c.CompanyId == m.CompanyId && c.Status == EntityStatus.Active
                )
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Department,
                    c.CampaignStatus,
                    c.EndDate,
                    c.Openings,
                    owner = c.OwnerRecruiter.Account.FullName,
                    applications = c.JobApplications.Count(a => a.Status == EntityStatus.Active),
                    hired = c.JobApplications.Count(a =>
                        a.Status == EntityStatus.Active && a.ApplicationStatus == "Hired"
                    ),
                    stages = c
                        .CampaignStages.Where(s => s.Status == EntityStatus.Active)
                        .OrderBy(s => s.StageOrder)
                        .Select(s => new
                        {
                            s.Id,
                            name = s.StageName,
                            s.StageOrder,
                            count = s.JobApplications.Count(a => a.Status == EntityStatus.Active),
                        }),
                })
                .ToListAsync(ct);
            var pending = await db.CandidateJobSubmissions.CountAsync(
                s =>
                    s.Job.CompanyId == m.CompanyId
                    && db.RecruitmentCampaignRecords.Any(c =>
                        c.Id == s.JobId && c.CompanyId == m.CompanyId
                    )
                    && !db.JobApplicationRecords.Any(a => a.Id == s.Id),
                ct
            );
            return Ok(new { campaigns, pendingImports = pending });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Metadata"
            );
            throw;
        }
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var tx = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                        : null;
                    var submissions = await db
                        .CandidateJobSubmissions.Where(s =>
                            s.Job.CompanyId == m.CompanyId
                            && db.RecruitmentCampaignRecords.Any(c =>
                                c.Id == s.JobId && c.CompanyId == m.CompanyId
                            )
                            && !db.JobApplicationRecords.Any(a => a.Id == s.Id)
                        )
                        .Select(s => new
                        {
                            s.Id,
                            s.JobId,
                            s.CandidateId,
                            s.CvId,
                            s.AppliedAt,
                            s.CoverLetter,
                            s.Status,
                        })
                        .Take(500)
                        .ToListAsync(ct);
                    foreach (var s in submissions)
                        db.JobApplicationRecords.Add(
                            new JobApplication
                            {
                                Id = s.Id,
                                CampaignId = s.JobId,
                                CandidateId = s.CandidateId,
                                CvId = s.CvId,
                                AppliedAt = s.AppliedAt,
                                CoverLetter = s.CoverLetter,
                                Source = "Website",
                                ApplicationStatus = s.Status == "hired" ? "Hired" : "Applied",
                            }
                        );
                    await db.SaveChangesAsync(ct);
                    if (tx is not null)
                        await tx.CommitAsync(ct);
                    return (IActionResult)Ok(new { imported = submissions.Count });
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Import"
            );
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? campaignId,
        [FromQuery] Guid? stageId,
        [FromQuery] string? keyword,
        [FromQuery] string? state,
        [FromQuery] string? source,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        CancellationToken ct = default
    )
    {
        try
        {
            if (
                page < 1
                || page > 100000
                || keyword?.Length > 200
                || state?.Length > 30
                || source?.Length > 200
                || !new[] { "newest", "name", "score" }.Contains(sort)
            )
                return BadRequest();
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var q = Applications(m.CompanyId);
            if (campaignId.HasValue)
                q = q.Where(a => a.CampaignId == campaignId);
            if (stageId.HasValue)
                q = q.Where(a => a.StageId == stageId);
            if (!string.IsNullOrEmpty(state))
                q = q.Where(a => a.ApplicationStatus == state);
            if (!string.IsNullOrEmpty(source))
                q = q.Where(a => a.Source == source);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();
                q = q.Where(a =>
                    a.Candidate.Account.FullName.Contains(term)
                    || (
                        a.Source != "Talent Pool"
                        && (
                            (
                                a.Candidate.Account.Email != null
                                && a.Candidate.Account.Email.Contains(term)
                            )
                            || (
                                a.Candidate.Account.Phone != null
                                && a.Candidate.Account.Phone.Contains(term)
                            )
                        )
                    )
                    || a.Candidate.CandidateSkills.Any(s =>
                        s.Status == EntityStatus.Active && s.Skill.SkillName.Contains(term)
                    )
                );
            }

            var total = await q.CountAsync(ct);
            q =
                sort == "name" ? q.OrderBy(a => a.Candidate.Account.FullName).ThenBy(a => a.Id)
                : sort == "score"
                    ? q.OrderByDescending(a => a.MatchScore)
                        .ThenByDescending(a => a.AppliedAt)
                        .ThenBy(a => a.Id)
                : q.OrderByDescending(a => a.AppliedAt).ThenBy(a => a.Id);
            var rows = await q.AsNoTrackingWithIdentityResolution()
                .AsSplitQuery()
                .Include(a => a.Candidate)
                .ThenInclude(p => p.Account)
                .Include(a => a.Candidate)
                .ThenInclude(p =>
                    p.CandidateSkills.Where(s =>
                        s.Status == EntityStatus.Active && s.Skill.Status == EntityStatus.Active
                    )
                )
                .ThenInclude(s => s.Skill)
                .Include(a => a.Stage)
                .Include(a => a.Campaign)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync(ct);
            var candidateIds = rows.Select(a => a.CandidateId).ToArray();
            var savedCandidateIds = (
                await db
                    .TalentPoolEntryRecords.AsNoTracking()
                    .Where(t =>
                        t.CompanyId == m.CompanyId
                        && t.Status == EntityStatus.Active
                        && candidateIds.Contains(t.CandidateId)
                    )
                    .Select(t => t.CandidateId)
                    .Distinct()
                    .ToListAsync(ct)
            ).ToHashSet();
            return Ok(
                new
                {
                    items = rows.Select(a => new
                    {
                        isSavedToTalentPool = savedCandidateIds.Contains(a.CandidateId),
                        a.Id,
                        a.CandidateId,
                        a.CampaignId,
                        campaign = a.Campaign.Title,
                        a.StageId,
                        stage = a.Stage?.StageName,
                        a.ApplicationStatus,
                        a.Source,
                        a.MatchScore,
                        a.AppliedAt,
                        revision = a.UpdatedDate,
                        profile = Profile(a.Candidate, a.Source != "Talent Pool"),
                        hasCv = a.CvId.HasValue,
                    }),
                    totalCount = total,
                    page,
                    pageSize = 10,
                }
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.List"
            );
            throw;
        }
    }

    private static object Profile(CandidateProfile p, bool revealContact = true)
    {
        try
        {
            string? avatar = null;
            CandidateProfileDocument? document = null;
            var skillNames = p
                .CandidateSkills.Where(s =>
                    s.Status == EntityStatus.Active && s.Skill.Status == EntityStatus.Active
                )
                .Select(s => s.Skill.SkillName)
                .ToList();
            try
            {
                if (p.DetailsJson is not null)
                {
                    using var json = JsonDocument.Parse(p.DetailsJson);
                    if (json.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in json.RootElement.EnumerateObject())
                        {
                            if (
                                field.Name.Equals("AvatarUrl", StringComparison.OrdinalIgnoreCase)
                                && field.Value.ValueKind == JsonValueKind.String
                            )
                                avatar = field.Value.GetString();
                            else if (
                                field.Name.Equals("Skills", StringComparison.OrdinalIgnoreCase)
                                && field.Value.ValueKind == JsonValueKind.Array
                            )
                            {
                                // Profile edits store professional skills in DetailsJson; whitelist only entry titles.
                                foreach (
                                    var entry in field
                                        .Value.EnumerateArray()
                                        .Where(e => e.ValueKind == JsonValueKind.Object)
                                )
                                foreach (var entryField in entry.EnumerateObject())
                                    if (
                                        entryField.Name.Equals(
                                            "Title",
                                            StringComparison.OrdinalIgnoreCase
                                        )
                                        && entryField.Value.ValueKind == JsonValueKind.String
                                        && entryField.Value.GetString() is { } skill
                                    )
                                        skillNames.Add(skill);
                            }
                        }
                    }
                    try
                    {
                        document = json.RootElement.Deserialize<CandidateProfileDocument>(
                            ProfileJsonOptions
                        );
                    }
                    catch (JsonException) { }
                }
            }
            catch (JsonException) { }

            return new
            {
                location = document?.Location,
                desiredPosition = document?.DesiredPosition,
                desiredLocation = document?.DesiredLocation,
                workType = document?.WorkType,
                experiences = document?.Experiences ?? [],
                education = document?.Education ?? [],
                projects = document?.Projects ?? [],
                certificates = document?.Certificates ?? [],
                name = p.Account.FullName,
                email = revealContact ? p.Account.Email : null,
                phone = revealContact ? p.Account.Phone : null,
                p.Headline,
                p.YearsExperience,
                p.Summary,
                p.ReadyStatus,
                avatarUrl = avatar,
                skills = skillNames
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .GroupBy(TalentDiscoveryMatching.Normalize)
                    .Select(g => g.First().Trim())
                    .OrderBy(s => s)
                    .ToArray(),
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Profile"
            );
            throw;
        }
    }

    [HttpGet("talents")]
    public async Task<IActionResult> TalentList(
        [FromQuery] string? keyword,
        [FromQuery] string? readiness,
        [FromQuery] int page = 1,
        CancellationToken ct = default
    )
    {
        try
        {
            if (page < 1 || page > 100000 || keyword?.Length > 200 || readiness?.Length > 30)
                return BadRequest();
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var all = Talents(m.CompanyId);
            var q = all;
            if (!string.IsNullOrEmpty(readiness))
                q = q.Where(t => t.ReadinessStatus == readiness);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();
                q = q.Where(t =>
                    t.Candidate.Account.FullName.Contains(term)
                    || (
                        t.Candidate.Account.Email != null
                        && t.Candidate.Account.Email.Contains(term)
                        && (
                            db.JobApplicationRecords.Any(a =>
                                a.CandidateId == t.CandidateId
                                && a.Campaign.CompanyId == m.CompanyId
                                && a.Status == EntityStatus.Active
                                && a.Source != "Talent Pool"
                            )
                            || db.CandidateJobSubmissions.Any(s =>
                                s.CandidateId == t.CandidateId && s.Job.CompanyId == m.CompanyId
                            )
                        )
                    )
                    || t.Candidate.CandidateSkills.Any(s =>
                        s.Status == EntityStatus.Active && s.Skill.SkillName.Contains(term)
                    )
                );
            }

            var total = await q.CountAsync(ct);
            var rows = await q.AsNoTrackingWithIdentityResolution()
                .AsSplitQuery()
                .Include(t => t.Candidate)
                .ThenInclude(p => p.Account)
                .Include(t => t.Candidate)
                .ThenInclude(p =>
                    p.CandidateSkills.Where(s =>
                        s.Status == EntityStatus.Active && s.Skill.Status == EntityStatus.Active
                    )
                )
                .ThenInclude(s => s.Skill)
                .OrderByDescending(t => t.AddedAt)
                .ThenBy(t => t.Id)
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync(ct);
            var pageCandidates = rows.Select(t => t.CandidateId).ToArray();
            var applicantIds = (
                await Applications(m.CompanyId)
                    .Where(a => pageCandidates.Contains(a.CandidateId) && a.Source != "Talent Pool")
                    .Select(a => a.CandidateId)
                    .Union(
                        db.CandidateJobSubmissions.Where(s =>
                                pageCandidates.Contains(s.CandidateId)
                                && s.Job.CompanyId == m.CompanyId
                            )
                            .Select(s => s.CandidateId)
                    )
                    .ToListAsync(ct)
            ).ToHashSet();
            var poolCounts = await all.GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Ready = group.Count(t => t.ReadinessStatus == "ready"),
                })
                .SingleOrDefaultAsync(ct);
            return Ok(
                new
                {
                    items = rows.Select(t => new
                    {
                        t.Id,
                        t.CandidateId,
                        t.ReadinessStatus,
                        t.Notes,
                        t.AddedAt,
                        revision = t.UpdatedDate,
                        profile = Profile(t.Candidate, applicantIds.Contains(t.CandidateId)),
                    }),
                    totalCount = total,
                    page,
                    pageSize = 10,
                    poolCount = poolCounts?.Total ?? 0,
                    readyCount = poolCounts?.Ready ?? 0,
                }
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.TalentList"
            );
            throw;
        }
    }

    [HttpPost("talents")]
    public async Task<IActionResult> AddTalent(AddTalentRequest input, CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var tx = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                        : null;
                    var application = await Applications(m.CompanyId)
                        .SingleOrDefaultAsync(a => a.Id == input.ApplicationId, ct);
                    if (application is null)
                        return (IActionResult)NotFound();
                    // Reuse inactive entries from discovery so notes, readiness and the talent ID survive unsaving.
                    var entries = await db
                        .TalentPoolEntryRecords.Where(t =>
                            t.CompanyId == m.CompanyId && t.CandidateId == application.CandidateId
                        )
                        .OrderBy(t => t.AddedAt)
                        .ThenBy(t => t.Id)
                        .ToListAsync(ct);
                    var talent = entries.FirstOrDefault(t => t.Status == EntityStatus.Active);
                    if (talent is null)
                    {
                        talent = entries.FirstOrDefault();
                        if (talent is null)
                        {
                            talent = new TalentPoolEntry
                            {
                                CompanyId = m.CompanyId,
                                CandidateId = application.CandidateId,
                                AddedByRecruiterId = m.Id,
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
                            talent.AddedByRecruiterId = m.Id;
                            talent.UpdatedDate = DateTimeOffset.UtcNow;
                            talent.UpdatedAccountId = AccountId;
                        }
                    }
                    await db.SaveChangesAsync(ct);
                    if (tx is not null)
                        await tx.CommitAsync(ct);
                    return Ok();
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.AddTalent"
            );
            throw;
        }
    }

    [HttpPut("talents/{id:guid}")]
    public async Task<IActionResult> UpdateTalent(Guid id, TalentUpdate input, CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var t = await Talents(m.CompanyId).SingleOrDefaultAsync(t => t.Id == id, ct);
            if (t is null)
                return NotFound();
            if (t.UpdatedDate != input.Revision)
                return Conflict();
            t.Notes = input.Notes;
            t.ReadinessStatus = input.Readiness;
            t.UpdatedDate = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Ok(new { revision = t.UpdatedDate });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.UpdateTalent"
            );
            throw;
        }
    }

    [HttpPost("talents/{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignRequest input, CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var tx = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                        : null;
                    var talent = await Talents(m.CompanyId)
                        .SingleOrDefaultAsync(t => t.Id == id, ct);
                    var campaign = await db.RecruitmentCampaignRecords.SingleOrDefaultAsync(
                        c =>
                            c.Id == input.CampaignId
                            && c.CompanyId == m.CompanyId
                            && c.Status == EntityStatus.Active,
                        ct
                    );
                    if (talent is null || campaign is null)
                        return (IActionResult)NotFound();
                    if (campaign.CampaignStatus == "Ended")
                        return BadRequest();
                    // A previous application can remain visible, but it does not authorize new sourcing after opt-out.
                    if (
                        !await db.CandidateProfileRecords.AnyAsync(
                            p =>
                                p.Id == talent.CandidateId
                                && p.Status == EntityStatus.Active
                                && p.Account.Status == EntityStatus.Active
                                && p.Account.AccountType == "Candidate"
                                && p.Account.AccountStatus == "Active"
                                && (p.ReadyStatus == "looking" || p.ReadyStatus == "considering"),
                            ct
                        )
                    )
                        return BadRequest(
                            new ProblemDetails
                            {
                                Detail = "Chỉ thêm hồ sơ đang mở cơ hội vào chiến dịch tuyển dụng.",
                            }
                        );
                    // Duplicate detection must include existing records hidden by discovery visibility rules.
                    if (
                        await db.JobApplicationRecords.AnyAsync(
                            a =>
                                a.Campaign.CompanyId == m.CompanyId
                                && a.CampaignId == campaign.Id
                                && a.CandidateId == talent.CandidateId
                                && a.Status == EntityStatus.Active,
                            ct
                        )
                    )
                        return Conflict(
                            new ProblemDetails { Detail = "Ứng viên đã có trong chiến dịch này." }
                        );
                    db.JobApplicationRecords.Add(
                        new JobApplication
                        {
                            CampaignId = campaign.Id,
                            CandidateId = talent.CandidateId,
                            Source = "Talent Pool",
                            ApplicationStatus = "Applied",
                            AppliedAt = DateTimeOffset.UtcNow,
                        }
                    );
                    await db.SaveChangesAsync(ct);
                    if (tx is not null)
                        await tx.CommitAsync(ct);
                    return Ok();
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Assign"
            );
            throw;
        }
    }

    [HttpPost("move")]
    public async Task<IActionResult> Move(MoveRequest input, CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var stage = await db
                .CampaignStageRecords.Include(s => s.Campaign)
                .SingleOrDefaultAsync(
                    s =>
                        s.Id == input.StageId
                        && s.Status == EntityStatus.Active
                        && s.Campaign.CompanyId == m.CompanyId,
                    ct
                );
            if (stage is null)
                return NotFound();
            var rows = await Applications(m.CompanyId)
                .Where(a => input.ApplicationIds.Contains(a.Id))
                .ToListAsync(ct);
            if (
                rows.Count != input.ApplicationIds.Distinct().Count()
                || rows.Any(a =>
                    a.CampaignId != stage.CampaignId || a.ApplicationStatus is "Hired" or "Rejected"
                )
            )
                return BadRequest();
            foreach (var a in rows)
            {
                a.StageId = stage.Id;
                a.ApplicationStatus = "Interview";
                a.UpdatedDate = DateTimeOffset.UtcNow;
                db.ApplicationStageHistoryRecords.Add(
                    new ApplicationStageHistory
                    {
                        ApplicationId = a.Id,
                        StageId = stage.Id,
                        ChangedByRecruiterId = m.Id,
                        Note = "Chuyển vòng tuyển dụng",
                    }
                );
            }

            var submissions = await db
                .CandidateJobSubmissions.Where(s =>
                    input.ApplicationIds.Contains(s.Id) && s.Job.CompanyId == m.CompanyId
                )
                .ToListAsync(ct);
            foreach (var s in submissions)
            {
                s.Status = "interview";
                s.ReviewedAt ??= DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Move"
            );
            throw;
        }
    }

    [HttpGet("{id:guid}/cv")]
    public async Task<IActionResult> Cv(Guid id, CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var a = await Applications(m.CompanyId)
                .AsNoTracking()
                .Include(a => a.Cv)
                .SingleOrDefaultAsync(a => a.Id == id, ct);
            if (a is null)
                return NotFound();
            var snapshot = await db
                .CandidateJobSubmissions.Where(s => s.Id == id && s.Job.CompanyId == m.CompanyId)
                .Select(s => new { title = s.CvTitle, content = s.CvContentJson })
                .SingleOrDefaultAsync(ct);
            if (snapshot is not null)
                return Ok(snapshot);
            return a.Cv is null
                ? NotFound()
                : Ok(
                    new
                    {
                        title = a.Cv.Title,
                        content = a.Cv.OnlineDocumentJson ?? a.Cv.ContentJson,
                        url = a.Cv.FileUrl,
                    }
                );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Cv"
            );
            throw;
        }
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ApplicationStatus(
        Guid id,
        StatusRequest input,
        CancellationToken ct
    )
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var application = await Applications(m.CompanyId)
                .SingleOrDefaultAsync(a => a.Id == id, ct);
            if (application is null)
                return NotFound();
            if (application.UpdatedDate != input.Revision)
                return Conflict(
                    new ProblemDetails
                    {
                        Detail = "Hồ sơ đã thay đổi. Vui lòng tải lại trước khi cập nhật.",
                    }
                );
            application.ApplicationStatus = input.State;
            application.UpdatedDate = DateTimeOffset.UtcNow;
            application.UpdatedAccountId = AccountId;
            var submission = await db.CandidateJobSubmissions.SingleOrDefaultAsync(
                s => s.Id == id && s.Job.CompanyId == m.CompanyId,
                ct
            );
            if (submission is not null)
            {
                submission.Status = input.State switch
                {
                    "Applied" => "submitted",
                    "Interview" => "interview",
                    "Offer" => "offered",
                    "Hired" => "hired",
                    _ => "rejected",
                };
                submission.ReviewedAt ??= DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.ApplicationStatus"
            );
            throw;
        }
    }

    [HttpGet("interviews")]
    public async Task<IActionResult> Calendar(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? keyword,
        CancellationToken ct
    )
    {
        try
        {
            if (
                from == default
                || to == DateOnly.MaxValue
                || to < from
                || to.DayNumber - from.DayNumber > 42
                || keyword?.Length > 200
            )
                return BadRequest();
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var start = new DateTimeOffset(
                from.ToDateTime(TimeOnly.MinValue),
                TimeSpan.FromHours(7)
            );
            var end = new DateTimeOffset(
                to.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.FromHours(7)
            );
            var q = Interviews(m.CompanyId)
                .Where(i => i.ScheduledStart >= start && i.ScheduledStart < end);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();
                q = q.Where(i =>
                    i.Application.Candidate.Account.FullName.Contains(term)
                    || (i.Location != null && i.Location.Contains(term))
                    || i.Application.Campaign.Title.Contains(term)
                );
            }

            return Ok(
                await q.OrderBy(i => i.ScheduledStart)
                    .Select(i => new
                    {
                        i.Id,
                        i.ApplicationId,
                        i.ScheduledStart,
                        i.ScheduledEnd,
                        i.MeetingUrl,
                        i.Location,
                        i.InterviewStatus,
                        candidate = i.Application.Candidate.Account.FullName,
                        headline = i.Application.Candidate.Headline,
                        campaign = i.Application.Campaign.Title,
                        stage = i.Stage.StageName,
                        feedbacks = i
                            .InterviewFeedbacks.Where(f => f.Status == EntityStatus.Active)
                            .Select(f => new
                            {
                                f.Score,
                                f.Decision,
                                f.Comment,
                                reviewer = f.Recruiter.Account.FullName,
                            }),
                    })
                    .ToListAsync(ct)
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Calendar"
            );
            throw;
        }
    }

    [HttpPost("interviews")]
    public async Task<IActionResult> Schedule(ScheduleRequest input, CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                        : null;
                    var a = await Applications(m.CompanyId)
                        .SingleOrDefaultAsync(a => a.Id == input.ApplicationId, ct);
                    if (a is null)
                        return NotFound();
                    if (
                        a.ApplicationStatus is "Rejected" or "Hired"
                        || !await db.CampaignStageRecords.AnyAsync(
                            s =>
                                s.Id == input.StageId
                                && s.CampaignId == a.CampaignId
                                && s.Status == EntityStatus.Active,
                            ct
                        )
                    )
                        return BadRequest();
                    if (
                        input.Start <= DateTimeOffset.UtcNow
                        || input.End <= input.Start
                        || input.End - input.Start > TimeSpan.FromHours(12)
                        || !ValidUrl(input.MeetingUrl)
                        || string.IsNullOrWhiteSpace(input.Location)
                            && string.IsNullOrWhiteSpace(input.MeetingUrl)
                    )
                        return BadRequest(
                            new ProblemDetails
                            {
                                Detail =
                                    "Kiểm tra thời gian tương lai, thời lượng tối đa 12 giờ và địa điểm/link họp hợp lệ.",
                            }
                        );
                    if (
                        await Interviews(m.CompanyId)
                            .AnyAsync(
                                i =>
                                    i.InterviewStatus != "cancelled"
                                    && i.ScheduledStart < input.End
                                    && i.ScheduledEnd > input.Start
                                    && (
                                        i.Application.CandidateId == a.CandidateId
                                        || (
                                            !string.IsNullOrEmpty(input.Location)
                                            && i.Location == input.Location
                                        )
                                    ),
                                ct
                            )
                    )
                        return Conflict(
                            new ProblemDetails
                            {
                                Detail = "Ứng viên hoặc phòng họp đã có lịch trùng thời gian.",
                            }
                        );
                    var interview = new Interview
                    {
                        ApplicationId = a.Id,
                        StageId = input.StageId,
                        ScheduledStart = input.Start,
                        ScheduledEnd = input.End,
                        Location = input.Location,
                        MeetingUrl = input.MeetingUrl,
                        InterviewStatus = "pending",
                        CreatedAccountId = AccountId,
                    };
                    db.InterviewRecords.Add(interview);
                    a.StageId = input.StageId;
                    a.ApplicationStatus = "Interview";
                    db.ApplicationStageHistoryRecords.Add(
                        new ApplicationStageHistory
                        {
                            ApplicationId = a.Id,
                            StageId = input.StageId,
                            ChangedByRecruiterId = m.Id,
                            Note = "Lên lịch phỏng vấn",
                        }
                    );
                    var submission = await db.CandidateJobSubmissions.SingleOrDefaultAsync(
                        s => s.Id == a.Id && s.Job.CompanyId == m.CompanyId,
                        ct
                    );
                    if (submission is not null)
                    {
                        submission.Status = "interview";
                        submission.ReviewedAt ??= DateTimeOffset.UtcNow;
                    }

                    await db.SaveChangesAsync(ct);
                    if (transaction is not null)
                        await transaction.CommitAsync(ct);
                    return (IActionResult)Ok(new { interview.Id });
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Schedule"
            );
            throw;
        }
    }

    [HttpPost("interviews/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var i = await Interviews(m.CompanyId).SingleOrDefaultAsync(i => i.Id == id, ct);
            if (i is null)
                return NotFound();
            if (i.InterviewStatus == "completed")
                return Conflict();
            i.InterviewStatus = "cancelled";
            i.UpdatedDate = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Cancel"
            );
            throw;
        }
    }

    [HttpPut("interviews/{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, FeedbackRequest input, CancellationToken ct)
    {
        try
        {
            var m = await Member(ct);
            if (m is null)
                return NotFound();
            var i = await Interviews(m.CompanyId).SingleOrDefaultAsync(i => i.Id == id, ct);
            if (i is null)
                return NotFound();
            if (i.InterviewStatus == "cancelled" || i.ScheduledStart > DateTimeOffset.UtcNow)
                return BadRequest();
            var feedback = await db.InterviewFeedbackRecords.SingleOrDefaultAsync(
                f =>
                    f.InterviewId == id && f.RecruiterId == m.Id && f.Status == EntityStatus.Active,
                ct
            );
            if (feedback is null)
            {
                feedback = new InterviewFeedback { InterviewId = id, RecruiterId = m.Id };
                db.InterviewFeedbackRecords.Add(feedback);
            }

            feedback.Score = input.Score;
            feedback.Decision = input.Decision;
            feedback.Comment = input.Comment;
            feedback.SubmittedAt = DateTimeOffset.UtcNow;
            i.InterviewStatus = "completed";
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.Feedback"
            );
            throw;
        }
    }

    private static bool ValidUrl(string? value)
    {
        try
        {
            return string.IsNullOrEmpty(value)
                || Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    && uri.Scheme is "https" or "http";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCandidatesController.ValidUrl"
            );
            throw;
        }
    }

    public sealed class AddTalentRequest
    {
        public Guid ApplicationId { get; set; }
    }

    public sealed class StatusRequest
    {
        [Required, RegularExpression("^(Applied|Interview|Offer|Hired|Rejected)$")]
        public string State { get; set; } = "Applied";
        public DateTimeOffset? Revision { get; set; }
    }

    public sealed class TalentUpdate
    {
        [Required, RegularExpression("^(unknown|ready|later|unavailable)$")]
        public string Readiness { get; set; } = "unknown";

        [StringLength(4000)]
        public string? Notes { get; set; }
        public DateTimeOffset? Revision { get; set; }
    }

    public sealed class AssignRequest
    {
        public Guid CampaignId { get; set; }
    }

    public sealed class MoveRequest
    {
        [Required, MinLength(1), MaxLength(100)]
        public List<Guid> ApplicationIds { get; set; } = [];
        public Guid StageId { get; set; }
    }

    public sealed class ScheduleRequest
    {
        public Guid ApplicationId { get; set; }
        public Guid StageId { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(2048)]
        public string? MeetingUrl { get; set; }
    }

    public sealed class FeedbackRequest
    {
        [Range(0, 10)]
        public decimal Score { get; set; }

        [Required, RegularExpression("^(Pass|Fail|Consider)$")]
        public string Decision { get; set; } = "Consider";

        [Required, StringLength(4000)]
        public string Comment { get; set; } = "";
    }
}
