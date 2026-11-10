using System.Security.Claims;
using System.Text.Json;
using JobTot.Application.Profiles;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/candidate/header"), Route("api/employer/header")]
[Authorize(
    AuthenticationSchemes = CandidateAuthentication.Scheme + "," + EmployerAuthentication.Scheme
)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountHeaderController(RecruitmentDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var details = await db
                .CandidateProfileRecords.AsNoTracking()
                .Where(p => p.AccountId == id && p.Status == EntityStatus.Active)
                .Select(p => p.DetailsJson)
                .SingleOrDefaultAsync(ct);
            var avatarUrl = details is null
                ? null
                : JsonSerializer.Deserialize<CandidateProfileDocument>(details)?.AvatarUrl;
            var recruiter = await db
                .RecruiterProfileRecords.AsNoTracking()
                .Where(p =>
                    p.AccountId == id
                    && p.Status == EntityStatus.Active
                    && p.Company.Status == EntityStatus.Active
                )
                .OrderBy(p => p.JoinedAt)
                .ThenBy(p => p.Id)
                .Select(p => new
                {
                    p.CompanyId,
                    p.Company.Culture,
                    LogoUrl = p.Company.LogoFile == null ? null : p.Company.LogoFile.Url,
                })
                .FirstOrDefaultAsync(ct);
            var companyLogoUrl = recruiter is null
                ? null
                : EmployerCompanyController
                    .ReadBrand(recruiter.Culture, recruiter.LogoUrl, null)
                    .LogoUrl;
            var messages = await db
                .ChatMessages.AsNoTracking()
                .Where(m =>
                    !db.AccountMessageReads.Any(r => r.AccountId == id && r.MessageId == m.Id)
                    && m.SenderAccountId != id
                    && (m.Conversation.FirstAccountId == id || m.Conversation.SecondAccountId == id)
                )
                .OrderByDescending(m => m.Id)
                .Take(50)
                .Select(m => new
                {
                    m.Id,
                    m.ConversationId,
                    m.SentAt,
                })
                .ToListAsync(ct);
            var notices = new List<HeaderNotice>();
            if (recruiter != null && Request.Path.StartsWithSegments("/api/employer"))
            {
                var submissions = db
                    .CandidateJobSubmissions.AsNoTracking()
                    .Where(s =>
                        s.Job.CompanyId == recruiter.CompanyId
                        && s.Candidate.Status == EntityStatus.Active
                        && s.Candidate.Account.Status == EntityStatus.Active
                    );
                var newSubmissions = await submissions
                    .OrderByDescending(s => s.AppliedAt)
                    .ThenBy(s => s.Id)
                    .Take(30)
                    .Select(s => new
                    {
                        s.Id,
                        s.AppliedAt,
                        s.Job.Title,
                        Candidate = s.FullName,
                    })
                    .ToListAsync(ct);
                notices.AddRange(
                    newSubmissions.Select(s => new HeaderNotice(
                        "application-" + s.Id,
                        "application",
                        "Ứng viên mới ứng tuyển",
                        $"{s.Candidate} đã nộp hồ sơ ứng tuyển vị trí {s.Title}.",
                        s.AppliedAt,
                        "/employer/candidates"
                    ))
                );
                // Imported submissions retain their ID in Application; show one notice per submission.
                var campaignApplications = await db
                    .JobApplicationRecords.AsNoTracking()
                    .Where(a =>
                        a.Status == EntityStatus.Active
                        && a.Campaign.Status == EntityStatus.Active
                        && a.Campaign.CompanyId == recruiter.CompanyId
                        && a.Candidate.Status == EntityStatus.Active
                        && a.Candidate.Account.Status == EntityStatus.Active
                        && !submissions.Any(s => s.Id == a.Id)
                    )
                    .OrderByDescending(a => a.AppliedAt)
                    .ThenBy(a => a.Id)
                    .Take(30)
                    .Select(a => new
                    {
                        a.Id,
                        a.AppliedAt,
                        a.Campaign.Title,
                        Candidate = a.Candidate.Account.FullName,
                        a.CampaignId,
                    })
                    .ToListAsync(ct);
                notices.AddRange(
                    campaignApplications.Select(a => new HeaderNotice(
                        "application-" + a.Id,
                        "application",
                        "Ứng viên mới ứng tuyển",
                        $"{a.Candidate} đã nộp hồ sơ ứng tuyển vị trí {a.Title}.",
                        a.AppliedAt,
                        "/employer/candidates"
                    ))
                );
            }
            var viewed = await db
                .CandidateJobSubmissions.AsNoTracking()
                .Where(s =>
                    s.Candidate.AccountId == id
                    && s.ReviewedAt != null
                    && s.Job.Company.Status == EntityStatus.Active
                )
                .OrderByDescending(s => s.ReviewedAt)
                .Take(20)
                .Select(s => new
                {
                    s.Id,
                    s.ReviewedAt,
                    s.Job.Title,
                    company = s.Job.Company.Name,
                })
                .ToListAsync(ct);
            notices.AddRange(
                viewed.Select(s => new HeaderNotice(
                    "view-" + s.Id,
                    "view",
                    "Hồ sơ đã được xem",
                    $"{s.company} đã xem hồ sơ ứng tuyển của bạn cho vị trí {s.Title}.",
                    s.ReviewedAt!.Value,
                    "/my-jobs"
                ))
            );
            var interviews = await db
                .InterviewRecords.AsNoTracking()
                .Where(i =>
                    i.Status == EntityStatus.Active
                    && i.Application.Status == EntityStatus.Active
                    && (
                        i.Application.Candidate.AccountId == id
                        || i.Application.Campaign.OwnerRecruiter.AccountId == id
                    )
                    && i.InterviewStatus != "cancelled"
                    && i.InterviewStatus != "canceled"
                )
                .OrderByDescending(i => i.CreatedDate)
                .Take(20)
                .Select(i => new
                {
                    i.Id,
                    i.CreatedDate,
                    i.ScheduledStart,
                    i.Application.Campaign.Title,
                    candidate = i.Application.Candidate.AccountId == id,
                })
                .ToListAsync(ct);
            notices.AddRange(
                interviews.Select(i => new HeaderNotice(
                    "interview-" + i.Id,
                    "interview",
                    "Lịch phỏng vấn",
                    $"{i.Title} • {i.ScheduledStart.ToOffset(TimeSpan.FromHours(7)):dd/MM/yyyy HH:mm}",
                    i.CreatedDate,
                    i.candidate ? "/my-jobs?tab=interviews" : "/employer/interviews"
                ))
            );
            var unreadConversationCount = await db
                .ChatConversations.AsNoTracking()
                .Where(c =>
                    (c.FirstAccountId == id || c.SecondAccountId == id)
                    && c.Messages.Any(m =>
                        m.SenderAccountId != id
                        && !db.AccountMessageReads.Any(r =>
                            r.AccountId == id && r.MessageId == m.Id
                        )
                    )
                )
                .CountAsync(ct);
            var latestNotices = notices.OrderByDescending(n => n.At).Take(30).ToArray();
            var notificationIds = latestNotices.Select(n => n.Id).ToArray();
            var readIds = (
                await db
                    .AccountNotificationReads.AsNoTracking()
                    .Where(r => r.AccountId == id && notificationIds.Contains(r.NotificationId))
                    .Select(r => r.NotificationId)
                    .ToListAsync(ct)
            ).ToHashSet();
            return Ok(
                new
                {
                    avatarUrl,
                    companyLogoUrl,
                    messages,
                    unreadConversationCount,
                    notifications = latestNotices.Select(n => new
                    {
                        n.Id,
                        n.Kind,
                        n.Title,
                        n.Text,
                        n.At,
                        n.Href,
                        isRead = readIds.Contains(n.Id),
                    }),
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
                "AccountHeaderController.Get"
            );
            throw;
        }
    }

    [HttpPut("read")]
    public async Task<IActionResult> MarkRead(ReadInput input, CancellationToken ct)
    {
        if (
            input.Ids is null
            || input.Ids.Length > 100
            || input.Ids.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100)
        )
            return BadRequest();
        var accountId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var existing = await db
                .AccountNotificationReads.Where(r =>
                    r.AccountId == accountId && input.Ids.Contains(r.NotificationId)
                )
                .Select(r => r.NotificationId)
                .ToListAsync(ct);
            db.AccountNotificationReads.AddRange(
                input
                    .Ids.Distinct()
                    .Except(existing)
                    .Select(id => new AccountNotificationRead
                    {
                        AccountId = accountId,
                        NotificationId = id,
                    })
            );
            try
            {
                await db.SaveChangesAsync(ct);
                return NoContent();
            }
            catch (DbUpdateException ex)
                when (attempt < 2
                    && ex.InnerException
                        is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }
                )
            {
                db.ChangeTracker.Clear();
            }
        }
        return Conflict();
    }

    public sealed record ReadInput(string[] Ids);

    public sealed record HeaderNotice(
        string Id,
        string Kind,
        string Title,
        string Text,
        DateTimeOffset At,
        string Href
    );
}
