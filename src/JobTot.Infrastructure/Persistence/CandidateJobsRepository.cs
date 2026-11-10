using System.Text.Json;
using JobTot.Application;
using JobTot.Application.Companies;
using JobTot.Application.Profiles;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

public sealed class CandidateJobsRepository(
    RecruitmentDbContext db,
    IRecruitmentRepository recruitment,
    ICandidateProfileRepository profiles
) : ICandidateJobsRepository
{
    private async Task<Guid> CandidateId(Guid accountId, CancellationToken ct) =>
        await db
            .CandidateProfileRecords.AsNoTracking()
            .Where(x =>
                x.AccountId == accountId
                && x.Status == EntityStatus.Active
                && x.Account.Status == EntityStatus.Active
            )
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(ct)
        ?? throw new NotFoundException("Không tìm thấy hồ sơ ứng viên.");

    private async Task<CandidateProfile> Candidate(Guid accountId, CancellationToken ct)
    {
        try
        {
            return await db.CandidateProfileRecords.SingleOrDefaultAsync(
                    x =>
                        x.AccountId == accountId
                        && x.Status == EntityStatus.Active
                        && x.Account.Status == EntityStatus.Active,
                    ct
                ) ?? throw new NotFoundException("Không tìm thấy hồ sơ ứng viên.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsRepository.Candidate"
            );
            throw;
        }
    }

    public async Task<IReadOnlyList<Guid>> SavedIdsAsync(Guid accountId, CancellationToken ct)
    {
        try
        {
            var candidateId = await CandidateId(accountId, ct);
            return await db
                .CandidateSavedJobs.AsNoTracking()
                .Where(x =>
                    x.CandidateId == candidateId && x.Job.Company.Status == EntityStatus.Active
                )
                .OrderByDescending(x => x.SavedAt)
                .ThenBy(x => x.JobId)
                .Select(x => x.JobId)
                .ToListAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsRepository.SavedIdsAsync"
            );
            throw;
        }
    }

    public async Task SetSavedAsync(Guid accountId, Guid jobId, bool saved, CancellationToken ct)
    {
        try
        {
            var candidateId = await CandidateId(accountId, ct);
            var existing = await db.CandidateSavedJobs.SingleOrDefaultAsync(
                x => x.CandidateId == candidateId && x.JobId == jobId,
                ct
            );
            if (!saved)
            {
                if (existing != null)
                {
                    db.CandidateSavedJobs.Remove(existing);
                    try
                    {
                        await db.SaveChangesAsync(ct);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        db.ChangeTracker.Clear();
                        if (
                            await db.CandidateSavedJobs.AnyAsync(
                                x => x.CandidateId == candidateId && x.JobId == jobId,
                                ct
                            )
                        )
                            throw;
                    }
                }

                return;
            }

            if (
                !await db.Jobs.AnyAsync(
                    x => x.Id == jobId && x.Company.Status == EntityStatus.Active,
                    ct
                )
            )
                throw new NotFoundException("Không tìm thấy việc làm.");
            if (existing != null)
                return;
            db.CandidateSavedJobs.Add(
                new CandidateSavedJob { CandidateId = candidateId, JobId = jobId }
            );
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException error)
                when (error.InnerException
                        is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }
                )
            {
                // PUT is idempotent, including concurrent saves of the same job.
                db.ChangeTracker.Clear();
                if (
                    !await db.CandidateSavedJobs.AnyAsync(
                        x => x.CandidateId == candidateId && x.JobId == jobId,
                        ct
                    )
                )
                    throw;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsRepository.SetSavedAsync"
            );
            throw;
        }
    }

    public async Task SetAvailabilityAsync(Guid accountId, bool openToWork, CancellationToken ct)
    {
        try
        {
            await (SetReadyStatusAsync(accountId, openToWork ? "looking" : "paused", ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsRepository.SetAvailabilityAsync"
            );
            throw;
        }
    }

    public async Task SetReadyStatusAsync(Guid accountId, string readyStatus, CancellationToken ct)
    {
        try
        {
            if (readyStatus is not ("looking" or "considering" or "paused"))
                throw new ArgumentException("Trạng thái tìm việc không hợp lệ.");
            var candidate = await Candidate(accountId, ct);
            candidate.ReadyStatus = readyStatus;
            if (candidate.DetailsJson != null)
            {
                var profile = JsonSerializer.Deserialize<CandidateProfileDocument>(
                    candidate.DetailsJson
                )!;
                profile.ReadyStatus = candidate.ReadyStatus;
                candidate.DetailsJson = JsonSerializer.Serialize(profile);
            }

            candidate.UpdatedDate = DateTimeOffset.UtcNow;
            candidate.UpdatedAccountId = accountId;
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsRepository.SetReadyStatusAsync"
            );
            throw;
        }
    }

    private async Task<IReadOnlyList<JobDto>> Jobs(Guid[] ids, CancellationToken ct)
    {
        try
        {
            if (ids.Length == 0)
                return [];
            // This is hydration of an already paged ID list; a second total-count query
            // and another SQL sort provide no information to the dashboard.
            var items = await RecruitmentRepository
                .JobDetails(
                    db.Jobs.AsNoTracking()
                        .Where(x => ids.Contains(x.Id) && x.Company.Status == EntityStatus.Active)
                )
                .ToListAsync(ct);
            var byId = items.ToDictionary(x => x.Id);
            return ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsRepository.Jobs"
            );
            throw;
        }
    }

    public async Task ConfirmInterviewAsync(Guid accountId, Guid interviewId, CancellationToken ct)
    {
        try
        {
            var candidateId = await CandidateId(accountId, ct);
            var interview =
                await db.InterviewRecords.SingleOrDefaultAsync(
                    x =>
                        x.Id == interviewId
                        && x.Application.CandidateId == candidateId
                        && x.Status == EntityStatus.Active
                        && x.Application.Status == EntityStatus.Active
                        && x.Stage.Status == EntityStatus.Active
                        && x.Application.Campaign.Status == EntityStatus.Active
                        && x.Application.Campaign.Company.Status == EntityStatus.Active,
                    ct
                ) ?? throw new NotFoundException("Không tìm thấy lịch phỏng vấn.");
            if (
                interview.ScheduledEnd <= DateTimeOffset.UtcNow
                || interview.InterviewStatus is "cancelled" or "canceled" or "declined"
            )
                throw new JobApplicationConflictException(
                    "Lịch phỏng vấn đã kết thúc hoặc bị hủy."
                );
            if (interview.InterviewStatus is "confirmed" or "accepted")
                return;
            if (
                interview.InterviewStatus
                is not (null or "pending" or "proposed" or "invited" or "scheduled")
            )
                throw new JobApplicationConflictException(
                    "Lịch phỏng vấn không ở trạng thái chờ xác nhận."
                );
            interview.InterviewStatus = "confirmed";
            interview.UpdatedDate = DateTimeOffset.UtcNow;
            interview.UpdatedAccountId = accountId;
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsRepository.ConfirmInterviewAsync"
            );
            throw;
        }
    }

    private static IQueryable<CandidateInterviewDto> InterviewDetails(
        IQueryable<Interview> interviews
    )
    {
        try
        {
            return interviews.Select(x => new CandidateInterviewDto(
                x.Id,
                x.Application.Campaign.Title,
                x.Application.Campaign.Company.Name,
                x.ScheduledStart,
                x.ScheduledEnd,
                x.Location,
                x.MeetingUrl,
                x.InterviewStatus
            )
            {
                CompanyId = x.Application.Campaign.CompanyId,
                CompanyLogoUrl =
                    x.Application.Campaign.Company.LogoFile == null
                        ? null
                        : x.Application.Campaign.Company.LogoFile.Url,
                CompanyVerified = x.Application.Campaign.Company.VerificationStatus == "Verified",
                StageName = x.Stage.StageName,
                StageOrder = x.Stage.StageOrder,
                RecruiterName = x.Application.Campaign.OwnerRecruiter.Account.FullName,
                RecruiterTitle = x.Application.Campaign.OwnerRecruiter.JobTitle,
                RecruiterEmail = x.Application.Campaign.OwnerRecruiter.Account.Email,
                Preparation = x.Application.Campaign.Requirements,
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
                "CandidateJobsRepository.InterviewDetails"
            );
            throw;
        }
    }

    public async Task<CandidateJobsDashboard> DashboardAsync(
        Guid accountId,
        CandidateJobsQuery query,
        CancellationToken ct
    )
    {
        try
        {
            // Only the ID is needed here. Do not buffer DetailsJson/avatar a second time
            // before the profile repository loads the dashboard identity.
            var candidate =
                await db
                    .CandidateProfileRecords.AsNoTracking()
                    .Where(x =>
                        x.AccountId == accountId
                        && x.Status == EntityStatus.Active
                        && x.Account.Status == EntityStatus.Active
                    )
                    .Select(x => new { x.Id })
                    .SingleOrDefaultAsync(ct)
                ?? throw new NotFoundException("Không tìm thấy hồ sơ ứng viên.");
            var profile =
                await profiles.GetSummaryAsync(accountId, ct)
                ?? throw new NotFoundException("Không tìm thấy hồ sơ ứng viên.");
            var now = DateTimeOffset.UtcNow;
            var saved = db
                .CandidateSavedJobs.AsNoTracking()
                .Where(x =>
                    x.CandidateId == candidate.Id && x.Job.Company.Status == EntityStatus.Active
                );
            var applications = db
                .CandidateJobSubmissions.AsNoTracking()
                .Where(x =>
                    x.CandidateId == candidate.Id && x.Job.Company.Status == EntityStatus.Active
                );
            var campaignApplications = db
                .JobApplicationRecords.AsNoTracking()
                .Where(x =>
                    x.CandidateId == candidate.Id
                    && x.Status == EntityStatus.Active
                    && x.Campaign.Status == EntityStatus.Active
                    && x.Campaign.Company.Status == EntityStatus.Active
                );
            // Import preserves the submission ID and uses its JobId as CampaignId.
            // Keep the submission once; overlapping IDs for unrelated positions remain separate.
            var standaloneCampaignApplications = campaignApplications.Where(a =>
                !applications.Any(s =>
                    s.Id == a.Id
                    && s.JobId == a.CampaignId
                    && s.Job.CompanyId == a.Campaign.CompanyId
                )
            );
            var applicationHistory = applications
                .Select(x => new
                {
                    x.Id,
                    PositionId = x.JobId,
                    x.Status,
                    x.AppliedAt,
                    IsCampaign = false,
                })
                .Concat(
                    standaloneCampaignApplications.Select(x => new
                    {
                        x.Id,
                        PositionId = x.CampaignId,
                        Status = x.ApplicationStatus ?? "Applied",
                        x.AppliedAt,
                        IsCampaign = true,
                    })
                );
            var employerInterests = db
                .EmployerCandidateInterests.AsNoTracking()
                .Where(x =>
                    x.CandidateId == candidate.Id && x.Company.Status == EntityStatus.Active
                );
            var followed = db
                .FollowedCompanyRecords.AsNoTracking()
                .Where(x =>
                    x.CandidateId == candidate.Id
                    && x.Status == EntityStatus.Active
                    && x.Company.Status == EntityStatus.Active
                );
            var interviews = db
                .InterviewRecords.AsNoTracking()
                .Where(x =>
                    x.Application.CandidateId == candidate.Id
                    && x.Status == EntityStatus.Active
                    && x.Application.Status == EntityStatus.Active
                    && x.Stage.Status == EntityStatus.Active
                    && x.Application.Campaign.Status == EntityStatus.Active
                    && x.Application.Campaign.Company.Status == EntityStatus.Active
                    && x.ScheduledEnd > now
                    && x.InterviewStatus != "cancelled"
                    && x.InterviewStatus != "canceled"
                    && x.InterviewStatus != "declined"
                );
            var applicationCounts = await applicationHistory
                .GroupBy(x => 1)
                .Select(g => new
                {
                    Applied = g.Count(),
                    Hired = g.Count(x => x.Status.ToLower() == "hired"),
                })
                .SingleOrDefaultAsync(ct);
            // Independent badge counts can share one roundtrip. Keep application history's
            // conditional aggregation separate so its UNION is evaluated only once.
            var counters = await db
                .CandidateProfileRecords.Where(x => x.Id == candidate.Id)
                .Select(_ => new
                {
                    Viewed = applications.Count(x => x.ReviewedAt != null),
                    Interviews = interviews.Count(),
                    Saved = saved.Count(),
                    Followed = followed.Count(),
                    Interested = employerInterests.Count(),
                })
                .SingleAsync(ct);
            var stats = new CandidateJobsStats(
                applicationCounts?.Applied ?? 0,
                counters.Viewed,
                counters.Interviews,
                counters.Saved,
                counters.Followed
            )
            {
                InterestedEmployers = counters.Interested,
                Hired = applicationCounts?.Hired ?? 0,
            };
            var offset = (query.Page - 1) * query.PageSize;
            IReadOnlyList<JobDto> jobs = [];
            IReadOnlyList<CandidateJobApplicationItem> applicationItems = [];
            var companies = new List<CompanyListingDto>();
            IReadOnlyList<CandidateInterviewDto> interviewItems = [];
            IReadOnlyList<CandidateEmployerInterestDto> interestItems = [];
            IReadOnlyList<CandidateInterviewCalendarEvent> calendarItems = [];
            CandidateInterviewDto? nextInterview = null;
            int total;
            switch (query.Tab)
            {
                case "applied":
                case "hired":
                    var history =
                        query.Tab == "hired"
                            ? applicationHistory.Where(x => x.Status.ToLower() == "hired")
                            : applicationHistory;
                    total = query.Tab == "hired" ? stats.Hired : stats.Applied;
                    var historyPage = await history
                        .OrderByDescending(x => x.AppliedAt)
                        .ThenBy(x => x.Id)
                        .ThenBy(x => x.IsCampaign)
                        .Skip(offset)
                        .Take(query.PageSize)
                        .ToListAsync(ct);
                    var submissionIds = historyPage
                        .Where(x => !x.IsCampaign)
                        .Select(x => x.Id)
                        .ToArray();
                    var submissions = await applications
                        .Where(x => submissionIds.Contains(x.Id))
                        .Select(x => new CandidateJobApplicationItem(
                            x.JobId,
                            x.Status,
                            x.AppliedAt,
                            x.ReviewedAt
                        )
                        {
                            Id = x.Id,
                            CvTitle = x.CvTitle,
                            RecruiterName = x.Job.RecruiterName,
                            RecruiterEmail = x.Job.RecruiterEmail,
                        })
                        .ToListAsync(ct);
                    var campaignIds = historyPage
                        .Where(x => x.IsCampaign)
                        .Select(x => x.Id)
                        .ToArray();
                    var campaigns = await campaignApplications
                        .Where(x => campaignIds.Contains(x.Id))
                        .Select(x => new
                        {
                            Application = new CandidateJobApplicationItem(
                                x.CampaignId,
                                x.ApplicationStatus ?? "Applied",
                                x.AppliedAt,
                                null
                            )
                            {
                                Id = x.Id,
                                DetailUrl = "/companies/" + x.Campaign.CompanyId,
                                RecruiterName = x.Campaign.OwnerRecruiter.Account.FullName,
                                RecruiterEmail = x.Campaign.OwnerRecruiter.Account.Email,
                            },
                            Job = new JobDto(
                                x.CampaignId,
                                x.Campaign.CompanyId,
                                x.Campaign.Title,
                                x.Campaign.Description ?? "",
                                x.Campaign.WorkAddress ?? "",
                                x.Campaign.SalaryVisible ? x.Campaign.SalaryMin : null,
                                x.Campaign.SalaryVisible ? x.Campaign.SalaryMax : null,
                                x.Campaign.CreatedDate,
                                x.Campaign.ExpiresAt ?? now,
                                x.Campaign.CampaignStatus == "Closed"
                            )
                            {
                                CompanyName = x.Campaign.Company.Name,
                                CompanyLogoUrl =
                                    x.Campaign.Company.LogoFile == null
                                        ? null
                                        : x.Campaign.Company.LogoFile.Url,
                            },
                        })
                        .ToListAsync(ct);
                    var allItems = submissions
                        .Select(x => (Key: (IsCampaign: false, x.Id), Item: x))
                        .Concat(
                            campaigns.Select(x =>
                                (Key: (IsCampaign: true, x.Application.Id), Item: x.Application)
                            )
                        )
                        .ToDictionary(x => x.Key, x => x.Item);
                    applicationItems = historyPage
                        .Select(x => allItems[(x.IsCampaign, x.Id)])
                        .Select(x =>
                            x with
                            {
                                Progress = CandidateApplicationProgress.From(
                                    x.Status,
                                    x.ReviewedAt
                                ),
                            }
                        )
                        .ToList();
                    var allJobs = (await Jobs(submissions.Select(x => x.JobId).ToArray(), ct))
                        .Concat(campaigns.Select(x => x.Job))
                        .DistinctBy(x => x.Id)
                        .ToDictionary(x => x.Id);
                    jobs = historyPage
                        .Select(x => allJobs[x.PositionId])
                        .DistinctBy(x => x.Id)
                        .ToList();
                    nextInterview = await InterviewDetails(
                            interviews
                                .Where(x => x.ScheduledStart >= now)
                                .OrderBy(x => x.ScheduledStart)
                                .ThenBy(x => x.Id)
                        )
                        .FirstOrDefaultAsync(ct);
                    break;
                case "companies":
                    total = stats.InterestedEmployers;
                    interestItems = await employerInterests
                        .OrderByDescending(x => x.InterestedAt)
                        .ThenBy(x => x.Id)
                        .Skip(offset)
                        .Take(query.PageSize)
                        .Select(x => new CandidateEmployerInterestDto(
                            x.Id,
                            x.CompanyId,
                            x.Company.Name,
                            x.Company.LogoFile == null ? null : x.Company.LogoFile.Url,
                            x.Company.AddressLine,
                            x.Company.CompanySizeMin,
                            x.Company.CompanySizeMax,
                            db.Jobs.Count(j =>
                                j.CompanyId == x.CompanyId && !j.IsClosed && j.ExpiresAt > now
                            ),
                            x.InterestedAt,
                            null
                        )
                        {
                            RecruiterName = x.InterestedByRecruiter.Account.FullName,
                            RecruiterTitle = x.InterestedByRecruiter.JobTitle,
                            RecruiterEmail = x.InterestedByRecruiter.Account.Email,
                        })
                        .ToListAsync(ct);
                    break;
                case "interviews":
                    var calendarOffset = TimeSpan.FromMinutes(query.TimeZoneOffsetMinutes);
                    if (query.CalendarMonth is { } month)
                    {
                        var monthDate = DateOnly.ParseExact(
                            month + "-01",
                            "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture
                        );
                        var monthStart = new DateTimeOffset(
                            monthDate.ToDateTime(TimeOnly.MinValue),
                            calendarOffset
                        );
                        var monthEnd = monthStart.AddMonths(1);
                        calendarItems = await interviews
                            .Where(x =>
                                x.ScheduledStart >= monthStart && x.ScheduledStart < monthEnd
                            )
                            .OrderBy(x => x.ScheduledStart)
                            .ThenBy(x => x.Id)
                            .Select(x => new CandidateInterviewCalendarEvent(
                                x.Id,
                                x.Application.Campaign.Title,
                                x.ScheduledStart,
                                x.MeetingUrl != null && x.MeetingUrl != ""
                            ))
                            .ToListAsync(ct);
                    }

                    var interviewList = interviews;
                    if (query.InterviewDate is { } selectedDate)
                    {
                        var dayStart = new DateTimeOffset(
                            selectedDate.ToDateTime(TimeOnly.MinValue),
                            calendarOffset
                        );
                        var dayEnd = dayStart.AddDays(1);
                        interviewList = interviewList.Where(x =>
                            x.ScheduledStart >= dayStart && x.ScheduledStart < dayEnd
                        );
                    }

                    total = await interviewList.CountAsync(ct);
                    interviewItems = await InterviewDetails(
                            interviewList
                                .OrderBy(x => x.ScheduledStart)
                                .ThenBy(x => x.Id)
                                .Skip(offset)
                                .Take(query.PageSize)
                        )
                        .ToListAsync(ct);
                    break;
                default:
                    total = stats.Saved;
                    jobs = await Jobs(
                        await saved
                            .OrderByDescending(x => x.SavedAt)
                            .ThenBy(x => x.JobId)
                            .Skip(offset)
                            .Take(query.PageSize)
                            .Select(x => x.JobId)
                            .ToArrayAsync(ct),
                        ct
                    );
                    break;
            }

            var deadlineIds = await saved
                .Where(x =>
                    !x.Job.IsClosed && x.Job.ExpiresAt > now && x.Job.ExpiresAt <= now.AddDays(7)
                )
                .OrderBy(x => x.Job.ExpiresAt)
                .ThenBy(x => x.JobId)
                .Take(2)
                .Select(x => x.JobId)
                .ToArrayAsync(ct);
            var deadlines = await Jobs(deadlineIds, ct);
            var recommendations = await recruitment.SearchJobsAsync(
                new JobSearch
                {
                    ProvinceId = profile.Profile.DesiredProvinceId ?? profile.Profile.ProvinceId,
                    PageSize = 3,
                },
                ct
            );
            return new(
                profile.Profile.FullName,
                profile.Profile.Headline,
                profile.Cvs.FirstOrDefault(x => x.IsDefault)?.Title
                    ?? profile.Cvs.FirstOrDefault()?.Title,
                profile.Profile.ReadyStatus == "looking",
                stats,
                query.Tab,
                query.Page,
                query.PageSize,
                total,
                jobs,
                applicationItems,
                companies,
                interviewItems,
                deadlines,
                recommendations.Items
            )
            {
                ReadyStatus = profile.Profile.ReadyStatus,
                AvatarUrl = profile.Profile.AvatarUrl,
                EmployerInterests = interestItems,
                InterviewCalendar = calendarItems,
                NextInterview = nextInterview,
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
                "CandidateJobsRepository.DashboardAsync"
            );
            throw;
        }
    }
}
