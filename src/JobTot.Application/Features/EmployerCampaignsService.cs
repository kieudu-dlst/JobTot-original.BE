using System.Data;
using JobTot.Application.Companies;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;

namespace JobTot.Application.Features;

public sealed class EmployerCampaignsService(
    IRecruitmentData db,
    ICurrentActor actor
) : ApplicationService(actor)
{
    private Guid AccountId => Actor.AccountId;

    private sealed record RecruiterMembership(Guid Id, Guid CompanyId, string? VerificationStatus);

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
                .Select(p => new RecruiterMembership(
                    p.Id,
                    p.CompanyId,
                    p.Company.VerificationStatus
                ))
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
                "EmployerCampaignsService.Member"
            );
            throw;
        }
    }

    private IQueryable<RecruitmentCampaign> Campaigns(Guid companyId)
    {
        try
        {
            return db.RecruitmentCampaignRecords.Where(c =>
                c.CompanyId == companyId && c.Status == EntityStatus.Active
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
                "EmployerCampaignsService.Campaigns"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Metadata(CancellationToken ct)
    {
        try
        {
            var member = await Member(ct);
            if (member is null)
                return NotFound();
            return Success(
                new
                {
                    industries = await db
                        .IndustryRecords.Where(x => x.Status == EntityStatus.Active)
                        .OrderBy(x => x.IndustryName)
                        .Select(x => new { x.Id, name = x.IndustryName })
                        .ToListAsync(ct),
                    provinces = await db
                        .ProvinceRecords.Where(x => x.Status == EntityStatus.Active)
                        .OrderBy(x => x.ProvinceName)
                        .Select(x => new { x.Id, name = x.ProvinceName })
                        .ToListAsync(ct),
                    employmentTypes = await db
                        .EmploymentTypeRecords.Where(x => x.Status == EntityStatus.Active)
                        .Select(x => new { x.Id, name = x.TypeName })
                        .ToListAsync(ct),
                    recruiters = await db
                        .RecruiterProfileRecords.Where(x =>
                            x.CompanyId == member.CompanyId
                            && x.Status == EntityStatus.Active
                            && x.Account.Status == EntityStatus.Active
                        )
                        .Select(x => new
                        {
                            x.Id,
                            name = x.Account.FullName,
                            x.JobTitle,
                        })
                        .ToListAsync(ct),
                    currentRecruiterId = member.Id,
                    isCompanyAdmin = true,
                    canPublish = member.VerificationStatus == "Verified",
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
                "EmployerCampaignsService.Metadata"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> List(
        string? keyword,
        string? state,
        int page = 1,
        CancellationToken ct = default
    )
    {
        try
        {
            if (
                page is < 1 or > 100000
                || keyword?.Length > 200
                || (
                    state is not null
                    && !new[] { "Draft", "Running", "Paused", "Ended" }.Contains(state)
                )
            )
                return Invalid();
            var member = await Member(ct);
            if (member is null)
                return NotFound();
            var all = Campaigns(member.CompanyId);
            var query = all;
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(c =>
                    c.Title.Contains(keyword.Trim())
                    || (c.Department != null && c.Department.Contains(keyword.Trim()))
                );
            if (state is not null)
                query = query.Where(c => c.CampaignStatus == state);
            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(c => c.CreatedDate)
                .ThenBy(c => c.Id)
                .Skip((page - 1) * 5)
                .Take(5)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Department,
                    c.WorkAddress,
                    c.CampaignStatus,
                    c.StartDate,
                    c.EndDate,
                    applications = c.JobApplications.Count(a => a.Status == EntityStatus.Active)
                        + db.CandidateJobSubmissions.Count(a =>
                            a.JobId == c.Id
                            && !db.JobApplicationRecords.Any(original => original.Id == a.Id)
                        ),
                    fresh = c.JobApplications.Count(a =>
                        a.Status == EntityStatus.Active && a.ApplicationStatus == "Applied"
                    )
                        + db.CandidateJobSubmissions.Count(a =>
                            a.JobId == c.Id
                            && a.Status == "submitted"
                            && !db.JobApplicationRecords.Any(original => original.Id == a.Id)
                        ),
                    interviews = c.JobApplications.Count(a =>
                        a.Status == EntityStatus.Active && a.ApplicationStatus == "Interview"
                    ),
                    offers = c.JobApplications.Count(a =>
                        a.Status == EntityStatus.Active && a.ApplicationStatus == "Offer"
                    ),
                })
                .ToListAsync(ct);
            var applications = db.JobApplicationRecords.Where(a =>
                a.Campaign.CompanyId == member.CompanyId && a.Status == EntityStatus.Active
            );
            var submissions = db.CandidateJobSubmissions.Where(a =>
                a.Job.CompanyId == member.CompanyId
                && !db.JobApplicationRecords.Any(original => original.Id == a.Id)
            );
            var applicationCounts = await applications
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Hired = group.Count(a => a.ApplicationStatus == "Hired"),
                })
                .SingleOrDefaultAsync(ct);
            var submissionCounts = await submissions
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Hired = group.Count(a => a.Status == "hired"),
                })
                .SingleOrDefaultAsync(ct);
            var count = (applicationCounts?.Total ?? 0) + (submissionCounts?.Total ?? 0);
            var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;
            var from = new DateTimeOffset(today, TimeSpan.FromHours(7));
            var to = from.AddDays(1);
            var interviews = await db
                .InterviewRecords.Where(i =>
                    i.Application.Campaign.CompanyId == member.CompanyId
                    && i.Status == EntityStatus.Active
                    && i.InterviewStatus != "Cancelled"
                    && i.InterviewStatus != "cancelled"
                    && i.ScheduledStart >= from
                    && i.ScheduledStart < to
                )
                .OrderBy(i => i.ScheduledStart)
                .Select(i => new
                {
                    i.Id,
                    i.ScheduledStart,
                    i.Location,
                    i.MeetingUrl,
                    name = i.Application.Candidate.Account.FullName,
                    title = i.Application.Campaign.Title,
                })
                .ToListAsync(ct);
            return Success(
                new
                {
                    items,
                    totalCount = total,
                    page,
                    pageSize = 5,
                    stats = new
                    {
                        running = await all.CountAsync(c => c.CampaignStatus == "Running", ct),
                        applications = count,
                        interviews = interviews.Count,
                        hired = (applicationCounts?.Hired ?? 0) + (submissionCounts?.Hired ?? 0),
                    },
                    interviews,
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
                "EmployerCampaignsService.List"
            );
            throw;
        }
    }

    private IQueryable<RecruitmentCampaign> Details(Guid companyId)
    {
        try
        {
            return Campaigns(companyId)
                .SplitQuery()
                .With(c => c.CampaignSkills)
                .ThenWith(s => s.Skill)
                .With(c => c.CampaignBenefits)
                .ThenWith(b => b.Benefit)
                .With(c => c.CampaignStages);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCampaignsService.Details"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Get(Guid id, CancellationToken ct)
    {
        try
        {
            var member = await Member(ct);
            if (member is null)
                return NotFound();
            var campaign = await Details(member.CompanyId)
                .ReadOnly()
                .SingleOrDefaultAsync(c => c.Id == id, ct);
            return campaign is null ? NotFound() : Success(new
            {
                id,
                document = Map(campaign)
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
                "EmployerCampaignsService.Get"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Create(EmployerCampaignRequest request, CancellationToken ct)
    {
        try
        {
            return await (Save(null, request, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCampaignsService.Create"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Update(
        Guid id,
        EmployerCampaignRequest request,
        CancellationToken ct
    )
    {
        try
        {
            return await (Save(id, request, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCampaignsService.Update"
            );
            throw;
        }
    }

    private async Task<UseCaseResult> Save(
        Guid? id,
        EmployerCampaignRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var member = await Member(ct);
            if (member is null)
                return NotFound();
            if (request.CampaignStatus == "Running")
            {
                if (
                    string.Join("|", request.Benefits.Select(b => b.Title)).Length > 1000
                    || string.Join(
                        "\n",
                        request.Stages.Select((s, i) => $"{i + 1}. {s.Name} – {s.Kind}")
                    ).Length > 4000
                )
                    return Invalid(
                        new FailureDetails
                        {
                            Detail =
                                "Tổng tiêu đề phúc lợi quá 1.000 ký tự hoặc quy trình tuyển dụng quá 4.000 ký tự.",
                        }
                    );
                if (member.VerificationStatus != "Verified")
                    return Invalid(
                        new FailureDetails
                        {
                            Detail =
                                "Doanh nghiệp cần được xác minh trước khi bắt đầu đăng tuyển. Bạn vẫn có thể lưu nháp.",
                        }
                    );
                var today = DateOnly.FromDateTime(
                    DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime
                );
                if (request.EndDate < today || request.StartDate > today)
                    return Invalid(
                        new FailureDetails
                        {
                            Detail = "Chỉ bắt đầu chiến dịch trong khoảng ngày tuyển dụng đã chọn.",
                        }
                    );
            }

            if (
                !await db.IndustryRecords.AnyAsync(
                    x => x.Id == request.IndustryId && x.Status == EntityStatus.Active,
                    ct
                )
                || !await db.ProvinceRecords.AnyAsync(
                    x => x.Id == request.ProvinceId && x.Status == EntityStatus.Active,
                    ct
                )
                || !await db.EmploymentTypeRecords.AnyAsync(
                    x => x.Id == request.EmploymentTypeId && x.Status == EntityStatus.Active,
                    ct
                )
                || !await db.RecruiterProfileRecords.AnyAsync(
                    x =>
                        x.Id == request.OwnerRecruiterId
                        && x.CompanyId == member.CompanyId
                        && x.Status == EntityStatus.Active
                        && x.Account.Status == EntityStatus.Active,
                    ct
                )
            )
                return Invalid(
                    new FailureDetails
                    {
                        Detail =
                            "Ngành nghề, địa điểm, hình thức làm việc hoặc HR phụ trách không hợp lệ.",
                    }
                );
            return await db
                .ExecuteAsync(async () =>
                {
                    await using var transaction = db.SupportsTransactions ? await db.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                        : null;
                    var campaign = id.HasValue
                        ? await Details(member.CompanyId).SingleOrDefaultAsync(c => c.Id == id, ct)
                        : new RecruitmentCampaign
                        {
                            CompanyId = member.CompanyId,
                            CreatedAccountId = AccountId,
                        };
                    if (campaign is null)
                        return (UseCaseResult)NotFound();
                    if (id.HasValue && campaign.UpdatedDate != request.Revision)
                        return Conflict(
                            new FailureDetails
                            {
                                Detail =
                                    "Chiến dịch đã được cập nhật. Vui lòng tải lại trước khi lưu.",
                            }
                        );
                    if (campaign.CampaignStatus == "Ended")
                        return Conflict(
                            new FailureDetails
                            {
                                Detail = "Chiến dịch đã kết thúc không thể chỉnh sửa hoặc mở lại.",
                            }
                        );
                    if (
                        request.CampaignStatus == "Draft"
                        && campaign.CampaignStatus is "Running" or "Paused"
                    )
                        return Invalid();
                    campaign.Title = request.Title.Trim();
                    campaign.Department = request.Department.Trim();
                    campaign.WorkAddress = request.WorkAddress.Trim();
                    campaign.Description = request.Description;
                    campaign.Requirements = request.Requirements;
                    campaign.OwnerRecruiterId = request.OwnerRecruiterId;
                    campaign.IndustryId = request.IndustryId;
                    campaign.ProvinceId = request.ProvinceId;
                    campaign.EmploymentTypeId = request.EmploymentTypeId;
                    campaign.WorkMode = request.WorkMode;
                    campaign.Seniority = request.Seniority;
                    campaign.Openings = request.Openings;
                    campaign.StartDate = request.StartDate;
                    campaign.EndDate = request.EndDate;
                    campaign.SalaryMin = request.SalaryMin;
                    campaign.SalaryMax = request.SalaryMax;
                    campaign.SalaryVisible = request.SalaryVisible;
                    campaign.AgeMin = request.AgeMin;
                    campaign.AgeMax = request.AgeMax;
                    campaign.GenderPreference = request.GenderPreference;
                    campaign.CampaignStatus = request.CampaignStatus;
                    campaign.UpdatedDate = DateTimeOffset.UtcNow;
                    campaign.UpdatedAccountId = AccountId;
                    foreach (var existing in campaign.CampaignSkills)
                        existing.Status = EntityStatus.Deleted;
                    foreach (var name in request.Skills.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        var link = new CampaignSkill
                        {
                            CampaignId = campaign.Id,
                            Skill = new Skill { SkillName = name.Trim() },
                            IsRequired = true,
                        };
                        campaign.CampaignSkills.Add(link);
                        db.CampaignSkillRecords.Add(link);
                    }

                    foreach (var existing in campaign.CampaignBenefits)
                        existing.Status = EntityStatus.Deleted;
                    foreach (var benefit in request.Benefits)
                    {
                        var link = new CampaignBenefit
                        {
                            CampaignId = campaign.Id,
                            Benefit = new Benefit
                            {
                                BenefitName = benefit.Title.Trim(),
                                Detail = benefit.Description,
                            },
                        };
                        campaign.CampaignBenefits.Add(link);
                        db.CampaignBenefitRecords.Add(link);
                    }

                    var oldStages = campaign.CampaignStages.ToList();
                    if (
                        request.Stages.Any(s =>
                            s.Id.HasValue && oldStages.All(old => old.Id != s.Id)
                        )
                    )
                        return Invalid(
                            new FailureDetails { Detail = "Vòng phỏng vấn không thuộc chiến dịch." }
                        );
                    foreach (
                        var stage in oldStages.Where(s =>
                            request.Stages.All(input => input.Id != s.Id)
                        )
                    )
                        stage.Status = EntityStatus.Inactive;
                    for (var i = 0; i < request.Stages.Count; i++)
                    {
                        var input = request.Stages[i];
                        var stage = input.Id.HasValue
                            ? oldStages.Single(s => s.Id == input.Id)
                            : new CampaignStage();
                        stage.StageName = input.Name.Trim();
                        stage.StageKind = input.Kind;
                        stage.StageOrder = i + 1;
                        stage.Status = EntityStatus.Active;
                        if (!input.Id.HasValue)
                        {
                            stage.CampaignId = campaign.Id;
                            campaign.CampaignStages.Add(stage);
                            db.CampaignStageRecords.Add(stage);
                        }
                    }

                    if (!id.HasValue)
                        db.RecruitmentCampaignRecords.Add(campaign);
                    var job = await db.Jobs.SingleOrDefaultAsync(
                        j => j.Id == campaign.Id && j.CompanyId == member.CompanyId,
                        ct
                    );
                    if (request.CampaignStatus == "Running")
                    {
                        var expiry = new DateTimeOffset(
                            request.EndDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                            TimeSpan.FromHours(7)
                        );
                        if (job is null)
                        {
                            job = new JobPost(
                                campaign.Id,
                                member.CompanyId,
                                campaign.Title,
                                request.Description,
                                campaign.WorkAddress!,
                                request.SalaryVisible ? request.SalaryMin : null,
                                request.SalaryVisible ? request.SalaryMax : null,
                                expiry
                            );
                            db.Jobs.Add(job);
                        }
                        else
                        {
                            job.ResumeCampaign();
                            job.Update(
                                campaign.Title,
                                request.Description,
                                campaign.WorkAddress!,
                                request.SalaryVisible ? request.SalaryMin : null,
                                request.SalaryVisible ? request.SalaryMax : null,
                                expiry
                            );
                        }

                        var owner = await db
                            .RecruiterProfileRecords.With(p => p.Account)
                            .SingleAsync(p => p.Id == request.OwnerRecruiterId, ct);
                        job.SetProvince(request.ProvinceId);
                        job.SetCandidateDetails(
                            request.Requirements,
                            string.Join(
                                "\n",
                                request.Stages.Select((s, i) => $"{i + 1}. {s.Name} – {s.Kind}")
                            ),
                            request.Openings,
                            request.GenderPreference,
                            request.AgeMin.HasValue ? $"{request.AgeMin} – {request.AgeMax}" : null,
                            owner.Account.FullName,
                            owner.JobTitle,
                            owner.Account.Email
                        );
                        var typeName = (
                            await db
                                .EmploymentTypeRecords.Where(t => t.Id == request.EmploymentTypeId)
                                .Select(t => t.TypeName)
                                .SingleAsync(ct)
                        ).ToLowerInvariant();
                        var employmentType =
                            typeName.Contains("part") || typeName.Contains("bán") ? "part-time"
                            : typeName.Contains("intern") || typeName.Contains("thực tập")
                                ? "internship"
                            : request.WorkMode == "remote" ? "remote"
                            : request.WorkMode == "hybrid" ? "flexible"
                            : "full-time";
                        job.SetDirectoryDetails(
                            null,
                            request.Seniority,
                            employmentType,
                            workSchedule: request.WorkMode,
                            benefits: string.Join("|", request.Benefits.Select(b => b.Title))
                        );
                        campaign.PublishedAt ??= DateTimeOffset.UtcNow;
                    }
                    else
                        job?.Close();
                    await db.SaveChangesAsync(ct);
                    if (transaction is not null)
                        await transaction.CommitAsync(ct);
                    return (UseCaseResult)Success(new
                    {
                        id = campaign.Id,
                        document = Map(campaign)
                    });
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
                "EmployerCampaignsService.Save"
            );
            throw;
        }
    }

    private static EmployerCampaignRequest Map(RecruitmentCampaign c)
    {
        try
        {
            return new()
            {
                Title = c.Title,
                Department = c.Department ?? "",
                WorkAddress = c.WorkAddress ?? "",
                Description = c.Description ?? "",
                Requirements = c.Requirements ?? "",
                IndustryId = c.IndustryId,
                ProvinceId = c.ProvinceId,
                EmploymentTypeId = c.EmploymentTypeId,
                OwnerRecruiterId = c.OwnerRecruiterId,
                WorkMode = c.WorkMode ?? "office",
                Seniority = c.Seniority ?? "employee",
                Openings = c.Openings,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                SalaryMin = c.SalaryMin,
                SalaryMax = c.SalaryMax,
                SalaryVisible = c.SalaryVisible,
                AgeMin = c.AgeMin,
                AgeMax = c.AgeMax,
                GenderPreference = c.GenderPreference ?? "any",
                CampaignStatus = c.CampaignStatus ?? "Draft",
                Revision = c.UpdatedDate,
                Skills = c
                    .CampaignSkills.Where(s => s.Status == EntityStatus.Active)
                    .Select(s => s.Skill.SkillName)
                    .ToList(),
                Benefits = c
                    .CampaignBenefits.Where(b => b.Status == EntityStatus.Active)
                    .Select(b => new EmployerCampaignBenefit
                    {
                        Title = b.Benefit.BenefitName,
                        Description = b.Benefit.Detail ?? "",
                    })
                    .ToList(),
                Stages = c
                    .CampaignStages.Where(s => s.Status == EntityStatus.Active)
                    .OrderBy(s => s.StageOrder)
                    .Select(s => new EmployerCampaignStage
                    {
                        Id = s.Id,
                        Name = s.StageName,
                        Kind = s.StageKind ?? "",
                    })
                    .ToList(),
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
                "EmployerCampaignsService.Map"
            );
            throw;
        }
    }
}
