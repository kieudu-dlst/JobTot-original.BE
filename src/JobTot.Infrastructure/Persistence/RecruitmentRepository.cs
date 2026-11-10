using JobTot.Application;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

internal sealed class RecruitmentRepository(RecruitmentDbContext db) : IRecruitmentRepository
{
    public async Task<Company?> GetCompanyAsync(Guid id, CancellationToken ct)
    {
        try
        {
            return await (
                db
                    .Companies.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == id && x.Status == EntityStatus.Active, ct)
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
                "RecruitmentRepository.GetCompanyAsync"
            );
            throw;
        }
    }

    public async Task<PageResult<CompanyDto>> GetCompaniesAsync(
        int page,
        int pageSize,
        CancellationToken ct,
        string? keyword = null
    )
    {
        try
        {
            var query = db.Companies.AsNoTracking().Where(c => c.Status == EntityStatus.Active);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();
                query = query.Where(x => x.Name.Contains(term));
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CompanyDto(x.Id, x.Name, x.Description))
                .ToListAsync(ct);
            return new(items, total, page, pageSize);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentRepository.GetCompaniesAsync"
            );
            throw;
        }
    }

    public async Task<JobPost?> GetJobAsync(Guid id, CancellationToken ct)
    {
        try
        {
            return await (
                db
                    .Jobs.Where(x => x.Company.Status == EntityStatus.Active)
                    .Include(x => x.Company)
                    .ThenInclude(x => x.LogoFile)
                    .Include(x => x.Company)
                    .ThenInclude(x => x.Industry)
                    .SingleOrDefaultAsync(x => x.Id == id, ct)
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
                "RecruitmentRepository.GetJobAsync"
            );
            throw;
        }
    }

    public async Task<PageResult<JobDto>> SearchJobsAsync(JobSearch query, CancellationToken ct)
    {
        try
        {
            var jobs = db.Jobs.AsNoTracking().Where(x => x.Company.Status == EntityStatus.Active);
            var now = DateTimeOffset.UtcNow;
            if (!query.IncludeClosed)
                jobs = jobs.Where(x => !x.IsClosed && x.ExpiresAt > now);
            if (query.CompanyId.HasValue)
                jobs = jobs.Where(x => x.CompanyId == query.CompanyId);
            if (query.JobIds.Length > 0)
                jobs = jobs.Where(x => query.JobIds.Contains(x.Id));
            if (query.IndustryId is { } industryId)
                jobs = jobs.Where(x =>
                    x.Company.IndustryId == industryId
                    || x.Company.CompanyIndustrys.Any(y => y.IndustryId == industryId)
                );
            if (query.VerifiedOnly)
                jobs = jobs.Where(x => x.Company.VerificationStatus == "Verified");
            if (query.ExperienceLevels.Length > 0)
                jobs = jobs.Where(x => query.ExperienceLevels.Contains(x.ExperienceLevel!));
            if (query.PositionLevels.Length > 0)
                jobs = jobs.Where(x => query.PositionLevels.Contains(x.PositionLevel!));
            if (query.EmploymentTypes.Length > 0)
                jobs = jobs.Where(x => query.EmploymentTypes.Contains(x.EmploymentType!));
            jobs = ApplySalaryBand(jobs, query.SalaryBand);
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                jobs = jobs.Where(x =>
                    x.Title.Contains(keyword)
                    || x.Description.Contains(keyword)
                    || x.Company.Name.Contains(keyword)
                );
            }

            if (query.ProvinceId is { } provinceId)
            {
                jobs = jobs.Where(x =>
                    x.ProvinceId == provinceId && x.Province!.Status == EntityStatus.Active
                );
            }
            else if (!string.IsNullOrWhiteSpace(query.Location))
            {
                var location = query.Location.Trim();
                jobs = jobs.Where(x =>
                    x.Province != null
                    && x.Province.Status == EntityStatus.Active
                    && (
                        x.Province.ProvinceName == location
                        || x.Province.ProvinceName == "Thành phố " + location
                        || x.Province.ProvinceName == "Tỉnh " + location
                    )
                );
            }

            var total = await jobs.CountAsync(ct);
            var items = await JobDetails(
                    jobs.OrderByDescending(x => x.CreatedAt)
                        .ThenBy(x => x.Id)
                        .Skip((query.Page - 1) * query.PageSize)
                        .Take(query.PageSize)
                )
                .ToListAsync(ct);
            return new(items, total, query.Page, query.PageSize);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentRepository.SearchJobsAsync"
            );
            throw;
        }
    }

    internal static IQueryable<JobDto> JobDetails(IQueryable<JobPost> jobs) =>
        jobs.Select(x => new JobDto(
            x.Id,
            x.CompanyId,
            x.Title,
            x.Description,
            x.Location,
            x.SalaryMin,
            x.SalaryMax,
            x.CreatedAt,
            x.ExpiresAt,
            x.IsClosed
        )
        {
            ProvinceId = x.ProvinceId,
            CompanyName = x.Company.Name,
            CompanyLogoUrl = x.Company.LogoFile != null ? x.Company.LogoFile.Url : null,
            IsVerified = x.Company.VerificationStatus == "Verified",
            Industry = x.Company.Industry != null ? x.Company.Industry.IndustryName : null,
            ExperienceLevel = x.ExperienceLevel,
            PositionLevel = x.PositionLevel,
            EmploymentType = x.EmploymentType,
            SalaryPeriod = x.SalaryPeriod,
            WorkSchedule = x.WorkSchedule,
            SalaryNote = x.SalaryNote,
            Benefits = x.Benefits,
        });

    public async Task<Guid?> ResolveJobProvinceAsync(
        Guid? provinceId,
        string location,
        CancellationToken ct
    )
    {
        try
        {
            var provinces = db
                .ProvinceRecords.AsNoTracking()
                .Where(x => x.Status == EntityStatus.Active);
            if (provinceId is { } id)
            {
                if (!await provinces.AnyAsync(x => x.Id == id, ct))
                    throw new ArgumentException("Province not found or inactive.");
                return id;
            }

            var name = location.Trim();
            var matches = await provinces
                .Where(x =>
                    x.ProvinceName == name
                    || x.ProvinceName == "Thành phố " + name
                    || x.ProvinceName == "Tỉnh " + name
                )
                .Select(x => x.Id)
                .Take(2)
                .ToListAsync(ct);
            return matches.Count == 1 ? matches[0] : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentRepository.ResolveJobProvinceAsync"
            );
            throw;
        }
    }

    private static IQueryable<JobPost> ApplySalaryBand(IQueryable<JobPost> jobs, string? band)
    {
        try
        {
            if (band == "negotiable")
                return jobs.Where(x => x.SalaryMin == null && x.SalaryMax == null);
            if (string.IsNullOrEmpty(band))
                return jobs;
            var monthly = jobs.Where(x =>
                x.SalaryPeriod == "monthly" && (x.SalaryMin != null || x.SalaryMax != null)
            );
            // Use interval overlap, with half-open band boundaries to handle exact 10/15/20/30 million values.
            return band switch
            {
                "under-10" => monthly.Where(x => (x.SalaryMin ?? x.SalaryMax) < 10000000),
                "10-15" => monthly.Where(x =>
                    (x.SalaryMin ?? x.SalaryMax) < 15000000
                    && (x.SalaryMax ?? x.SalaryMin) >= 10000000
                ),
                "15-20" => monthly.Where(x =>
                    (x.SalaryMin ?? x.SalaryMax) < 20000000
                    && (x.SalaryMax ?? x.SalaryMin) >= 15000000
                ),
                "20-30" => monthly.Where(x =>
                    (x.SalaryMin ?? x.SalaryMax) <= 30000000
                    && (x.SalaryMax ?? x.SalaryMin) >= 20000000
                ),
                "over-30" => monthly.Where(x => (x.SalaryMax ?? x.SalaryMin) > 30000000),
                _ => monthly.Where(x => false),
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
                "RecruitmentRepository.ApplySalaryBand"
            );
            throw;
        }
    }

    public async Task<JobDirectoryMetadata> GetJobFiltersAsync(CancellationToken ct)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var jobs = db
                .Jobs.AsNoTracking()
                .Where(x =>
                    !x.IsClosed && x.ExpiresAt > now && x.Company.Status == EntityStatus.Active
                );
            // All fixed directory facets share the same filtered job set. Count them in one
            // SQL aggregation rather than scanning it once for every salary band and facet.
            var counts = await jobs.Select(x => new
                {
                    Monthly = x.SalaryPeriod == "monthly"
                        && (x.SalaryMin != null || x.SalaryMax != null),
                    MinSalary = x.SalaryMin ?? x.SalaryMax,
                    MaxSalary = x.SalaryMax ?? x.SalaryMin,
                    x.ExperienceLevel,
                    x.PositionLevel,
                    x.EmploymentType,
                })
                .GroupBy(x => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Under10 = g.Count(x => x.Monthly && x.MinSalary < 10000000),
                    From10To15 = g.Count(x =>
                        x.Monthly && x.MinSalary < 15000000 && x.MaxSalary >= 10000000
                    ),
                    From15To20 = g.Count(x =>
                        x.Monthly && x.MinSalary < 20000000 && x.MaxSalary >= 15000000
                    ),
                    From20To30 = g.Count(x =>
                        x.Monthly && x.MinSalary <= 30000000 && x.MaxSalary >= 20000000
                    ),
                    Over30 = g.Count(x => x.Monthly && x.MaxSalary > 30000000),
                    Negotiable = g.Count(x => x.MinSalary == null),
                    NoExperience = g.Count(x => x.ExperienceLevel == "none"),
                    ExperienceUnder1 = g.Count(x => x.ExperienceLevel == "under-1"),
                    Experience1To3 = g.Count(x => x.ExperienceLevel == "1-3"),
                    Experience3To5 = g.Count(x => x.ExperienceLevel == "3-5"),
                    ExperienceOver5 = g.Count(x => x.ExperienceLevel == "over-5"),
                    Interns = g.Count(x => x.PositionLevel == "intern"),
                    Employees = g.Count(x => x.PositionLevel == "employee"),
                    Supervisors = g.Count(x => x.PositionLevel == "supervisor"),
                    Managers = g.Count(x => x.PositionLevel == "manager"),
                    Directors = g.Count(x => x.PositionLevel == "director"),
                    FullTime = g.Count(x => x.EmploymentType == "full-time"),
                    PartTime = g.Count(x => x.EmploymentType == "part-time"),
                    Internships = g.Count(x => x.EmploymentType == "internship"),
                    Remote = g.Count(x => x.EmploymentType == "remote"),
                    Flexible = g.Count(x => x.EmploymentType == "flexible"),
                })
                .SingleOrDefaultAsync(ct);

            return new(
                counts?.Total ?? 0,
                await db.Companies.CountAsync(x => x.Status == EntityStatus.Active, ct),
                [
                    new("under-10", "Dưới 10 triệu", counts?.Under10 ?? 0),
                    new("10-15", "10 – 15 triệu", counts?.From10To15 ?? 0),
                    new("15-20", "15 – 20 triệu", counts?.From15To20 ?? 0),
                    new("20-30", "20 – 30 triệu", counts?.From20To30 ?? 0),
                    new("over-30", "Trên 30 triệu", counts?.Over30 ?? 0),
                    new("negotiable", "Thỏa thuận", counts?.Negotiable ?? 0),
                ],
                [
                    new("none", "Chưa có kinh nghiệm", counts?.NoExperience ?? 0),
                    new("under-1", "Dưới 1 năm", counts?.ExperienceUnder1 ?? 0),
                    new("1-3", "1 – 3 năm", counts?.Experience1To3 ?? 0),
                    new("3-5", "3 – 5 năm", counts?.Experience3To5 ?? 0),
                    new("over-5", "Trên 5 năm", counts?.ExperienceOver5 ?? 0),
                ],
                [
                    new("intern", "Thực tập sinh / Sinh viên", counts?.Interns ?? 0),
                    new("employee", "Nhân viên", counts?.Employees ?? 0),
                    new("supervisor", "Trưởng nhóm / Giám sát", counts?.Supervisors ?? 0),
                    new("manager", "Quản lý / Trưởng phòng", counts?.Managers ?? 0),
                    new("director", "Giám đốc & Cấp cao", counts?.Directors ?? 0),
                ],
                [
                    new("full-time", "Toàn thời gian", counts?.FullTime ?? 0),
                    new("part-time", "Bán thời gian", counts?.PartTime ?? 0),
                    new("internship", "Thực tập", counts?.Internships ?? 0),
                    new("remote", "Remote / Từ xa", counts?.Remote ?? 0),
                    new("flexible", "Hybrid linh hoạt", counts?.Flexible ?? 0),
                ]
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
                "RecruitmentRepository.GetJobFiltersAsync"
            );
            throw;
        }
    }

    public async Task<JobSubmissionDto?> GetSubmissionAsync(
        Guid accountId,
        Guid jobId,
        CancellationToken ct
    )
    {
        try
        {
            return await db
                .CandidateJobSubmissions.AsNoTracking()
                .Where(x =>
                    x.JobId == jobId
                    && x.Candidate.AccountId == accountId
                    && x.Candidate.Status == EntityStatus.Active
                    && x.Candidate.Account.Status == EntityStatus.Active
                )
                .Select(x => new JobSubmissionDto(
                    x.Id,
                    x.JobId,
                    x.CvId,
                    x.CvTitle,
                    x.AppliedAt,
                    x.Status
                ))
                .SingleOrDefaultAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentRepository.GetSubmissionAsync"
            );
            throw;
        }
    }

    public async Task<JobSubmissionDto> SubmitAsync(
        Guid accountId,
        Guid jobId,
        SubmitJobApplicationRequest input,
        CancellationToken ct
    )
    {
        try
        {
            var job =
                await db
                    .Jobs.AsNoTracking()
                    .Where(x => x.Id == jobId && x.Company.Status == EntityStatus.Active)
                    .Select(x => new { x.IsClosed, x.ExpiresAt })
                    .SingleOrDefaultAsync(ct)
                ?? throw new NotFoundException("Không tìm thấy việc làm.");
            if (job.IsClosed || job.ExpiresAt <= DateTimeOffset.UtcNow)
                throw new JobApplicationConflictException("Việc làm đã ngừng nhận hồ sơ.");
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
            if (
                await db.CandidateJobSubmissions.AnyAsync(
                    x => x.JobId == jobId && x.CandidateId == candidate.Id,
                    ct
                )
            )
                throw new JobApplicationConflictException("Bạn đã ứng tuyển việc làm này.");
            var cv = await db
                .CandidateCvRecords.AsNoTracking()
                .Where(x =>
                    x.Id == input.CvId
                    && x.CandidateId == candidate.Id
                    && x.Status == EntityStatus.Active
                )
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    ContentJson = x.OnlineDocumentJson ?? x.ContentJson,
                })
                .SingleOrDefaultAsync(ct);
            if (cv is null || cv.ContentJson is null)
                throw new ArgumentException("Vui lòng chọn CV hợp lệ thuộc hồ sơ của bạn.");
            if (string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(input.Phone))
                throw new ArgumentException("Vui lòng nhập họ tên và số điện thoại.");
            var submission = new CandidateJobSubmission
            {
                JobId = jobId,
                CandidateId = candidate.Id,
                CvId = cv.Id,
                CvTitle = cv.Title,
                // Online documents are JSON objects; uploaded PDFs remain their legacy Base64 JSON string.
                CvContentJson = cv.ContentJson,
                FullName = input.FullName.Trim(),
                Phone = input.Phone.Trim(),
                CoverLetter = input.CoverLetter?.Trim(),
            };
            db.CandidateJobSubmissions.Add(submission);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException error)
                when (error.InnerException
                        is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }
                )
            {
                throw new JobApplicationConflictException("Bạn đã ứng tuyển việc làm này.");
            }

            return new(
                submission.Id,
                submission.JobId,
                submission.CvId,
                submission.CvTitle,
                submission.AppliedAt,
                submission.Status
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
                "RecruitmentRepository.SubmitAsync"
            );
            throw;
        }
    }

    public void AddCompany(Company company)
    {
        try
        {
            db.Companies.Add(company);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentRepository.AddCompany"
            );
            throw;
        }
    }

    public void AddJob(JobPost job)
    {
        try
        {
            db.Jobs.Add(job);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentRepository.AddJob"
            );
            throw;
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
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
                "RecruitmentRepository.SaveChangesAsync"
            );
            throw;
        }
    }
}
