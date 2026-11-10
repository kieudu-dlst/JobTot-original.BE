using JobTot.Application;
using JobTot.Application.Companies;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

public sealed class CompanyDirectoryRepository(RecruitmentDbContext db)
    : ICompanyDirectoryRepository
{
    private sealed record HiringTitleRow(Guid CompanyId, string Title);

    private IQueryable<Company> Companies =>
        db.Companies.AsNoTracking().Where(x => x.Status == EntityStatus.Active);
    private IQueryable<JobPost> OpenJobs =>
        db.Jobs.AsNoTracking().Where(x => !x.IsClosed && x.ExpiresAt > DateTimeOffset.UtcNow);

    public async Task<PageResult<CompanyListingDto>> SearchAsync(
        CompanySearch search,
        CancellationToken ct
    )
    {
        try
        {
            var query = Companies;
            if (search.CompanyId is { } companyId)
                query = query.Where(x => x.Id == companyId);
            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var keyword = search.Keyword.Trim();
                query = query.Where(x =>
                    x.Name.Contains(keyword) || x.Description.Contains(keyword)
                );
            }

            if (search.ProvinceId is { } province)
                query = query.Where(x => x.HeadOfficeProvinceId == province);
            if (search.IndustryId is { } industry)
                query = query.Where(x =>
                    x.IndustryId == industry
                    || x.CompanyIndustrys.Any(y =>
                        y.IndustryId == industry && y.Status == EntityStatus.Active
                    )
                );
            if (search.VerifiedOnly)
                query = query.Where(x => x.VerificationStatus == "Verified");
            if (search.HiringOnly)
                query = query.Where(x => OpenJobs.Any(job => job.CompanyId == x.Id));
            if (search.CompanySizeMin is { } min)
                query = query.Where(x => (x.CompanySizeMax ?? x.CompanySizeMin) >= min);
            if (search.CompanySizeMax is { } max)
                query = query.Where(x => (x.CompanySizeMin ?? x.CompanySizeMax) <= max);
            var total = await query.CountAsync(ct);
            if (total == 0)
                return new([], 0, search.Page, search.PageSize);
            var ranked = query.Select(x => new
            {
                Company = x,
                OpenJobCount = OpenJobs.Count(job => job.CompanyId == x.Id),
            });
            var ordered = search.Sort switch
            {
                "name" => ranked.OrderBy(x => x.Company.Name).ThenBy(x => x.Company.Id),
                "jobs" => ranked
                    .OrderByDescending(x => x.OpenJobCount)
                    .ThenBy(x => x.Company.Name)
                    .ThenBy(x => x.Company.Id),
                _ => ranked
                    .OrderByDescending(x => x.Company.VerificationStatus == "Verified")
                    .ThenByDescending(x => x.OpenJobCount)
                    .ThenBy(x => x.Company.Name)
                    .ThenBy(x => x.Company.Id),
            };
            var items = await ordered
                .Skip((search.Page - 1) * search.PageSize)
                .Take(search.PageSize)
                .Select(x => new CompanyListingDto(
                    x.Company.Id,
                    x.Company.Name,
                    x.Company.Description,
                    x.Company.Industry != null ? x.Company.Industry.IndustryName : null,
                    x.Company.HeadOfficeProvince != null
                        ? x.Company.HeadOfficeProvince.ProvinceName
                        : null,
                    x.Company.AddressLine,
                    x.Company.CompanySizeMin,
                    x.Company.CompanySizeMax,
                    x.Company.LogoFile != null ? x.Company.LogoFile.Url : null,
                    x.Company.CoverFile != null ? x.Company.CoverFile.Url : null,
                    x.Company.VerificationStatus == "Verified",
                    x.OpenJobCount,
                    new List<string>()
                ))
                .ToListAsync(ct);
            var ids = items.Select(x => x.Id).ToArray();
            if (ids.Length == 0)
                return new([], total, search.Page, search.PageSize);
            var companyIdsJson = System.Text.Json.JsonSerializer.Serialize(ids);
            // Rank distinct titles before limiting each company. Binary collation plus byte length
            // preserves the previous ordinal, case-sensitive Distinct (including trailing spaces).
            var titles = await db
                .Database.SqlQuery<HiringTitleRow>(
                    $"""
                    SELECT [ranked].[CompanyId], [ranked].[Title]
                    FROM (
                        SELECT [latest].[CompanyId], [latest].[Title], [latest].[CreatedAt], [latest].[Id],
                            ROW_NUMBER() OVER (PARTITION BY [latest].[CompanyId] ORDER BY [latest].[CreatedAt] DESC, [latest].[Id]) AS [TitleRank]
                        FROM (
                            SELECT [j].[CompanyId], [j].[Title], [j].[CreatedAt], [j].[Id],
                                ROW_NUMBER() OVER (
                                    PARTITION BY [j].[CompanyId], [j].[Title] COLLATE Latin1_General_100_BIN2, DATALENGTH([j].[Title])
                                    ORDER BY [j].[CreatedAt] DESC, [j].[Id]) AS [DuplicateRank]
                            FROM [Jobs] AS [j]
                            WHERE [j].[IsClosed] = 0 AND [j].[ExpiresAt] > CAST(SYSUTCDATETIME() AS datetimeoffset)
                                AND [j].[CompanyId] IN (SELECT [id].[Value] FROM OPENJSON({companyIdsJson}) WITH ([Value] uniqueidentifier '$') AS [id])
                        ) AS [latest]
                        WHERE [latest].[DuplicateRank] = 1
                    ) AS [ranked]
                    WHERE [ranked].[TitleRank] <= 3
                    ORDER BY [ranked].[CreatedAt] DESC, [ranked].[Id]
                    """
                )
                .ToListAsync(ct);
            var byCompany = titles
                .GroupBy(x => x.CompanyId)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<string>)x.Select(y => y.Title).ToArray()
                );
            if (search.CompanyId is { } detailId && items.Count == 1)
            {
                var culture = await Companies
                    .Where(c => c.Id == detailId)
                    .Select(c => c.Culture)
                    .SingleOrDefaultAsync(ct);
                if (culture?.TrimStart().StartsWith('{') == true)
                {
                    try
                    {
                        var brand =
                            System.Text.Json.JsonSerializer.Deserialize<EmployerCompanyBrand>(
                                culture,
                                new System.Text.Json.JsonSerializerOptions(
                                    System.Text.Json.JsonSerializerDefaults.Web
                                )
                            );
                        if (brand is { SchemaVersion: 1 })
                            items[0] = items[0] with
                            {
                                Brand = brand,
                                CoverUrl = string.IsNullOrEmpty(brand.CoverUrl)
                                    ? items[0].CoverUrl
                                    : brand.CoverUrl,
                                LogoUrl = string.IsNullOrEmpty(brand.LogoUrl)
                                    ? items[0].LogoUrl
                                    : brand.LogoUrl,
                                Industry = string.IsNullOrEmpty(brand.Industry)
                                    ? items[0].Industry
                                    : brand.Industry,
                            };
                    }
                    catch (System.Text.Json.JsonException)
                    { /* Legacy culture is free text. */
                    }
                }
            }

            return new(
                items
                    .Select(x => x with { HiringTitles = byCompany.GetValueOrDefault(x.Id, []) })
                    .ToArray(),
                total,
                search.Page,
                search.PageSize
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
                "CompanyDirectoryRepository.SearchAsync"
            );
            throw;
        }
    }

    public async Task<CompanyDirectoryMetadata> MetadataAsync(CancellationToken ct)
    {
        try
        {
            var companyCounts = await Companies
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Verified = group.Count(x => x.VerificationStatus == "Verified"),
                })
                .SingleOrDefaultAsync(ct);
            var hiringCounts = await OpenJobs
                .Where(job => Companies.Any(company => company.Id == job.CompanyId))
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Companies = group.Select(job => job.CompanyId).Distinct().Count(),
                    Jobs = group.Count(),
                })
                .SingleOrDefaultAsync(ct);
            var stats = new CompanyDirectoryStats(
                companyCounts?.Total ?? 0,
                companyCounts?.Verified ?? 0,
                hiringCounts?.Companies ?? 0,
                hiringCounts?.Jobs ?? 0
            );
            var provinces = await db
                .ProvinceRecords.AsNoTracking()
                .Where(x => x.Status == EntityStatus.Active)
                .Select(x => new
                {
                    x.Id,
                    Name = x.ProvinceName,
                    Count = Companies.Count(c => c.HeadOfficeProvinceId == x.Id),
                })
                .OrderBy(x => x.Name)
                .Select(x => new CompanyFilterOption(x.Id, x.Name, x.Count))
                .ToListAsync(ct);
            var industries = await db
                .IndustryRecords.AsNoTracking()
                .Where(x => x.Status == EntityStatus.Active)
                .Select(x => new
                {
                    x.Id,
                    Name = x.IndustryName,
                    Count = Companies.Count(c =>
                        c.IndustryId == x.Id
                        || c.CompanyIndustrys.Any(y =>
                            y.IndustryId == x.Id && y.Status == EntityStatus.Active
                        )
                    ),
                })
                .Where(x => x.Count > 0)
                .OrderBy(x => x.Name)
                .Select(x => new CompanyFilterOption(x.Id, x.Name, x.Count))
                .ToListAsync(ct);
            return new(stats, provinces, industries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CompanyDirectoryRepository.MetadataAsync"
            );
            throw;
        }
    }

    public async Task<IReadOnlyList<Guid>> FollowedAsync(Guid accountId, CancellationToken ct)
    {
        try
        {
            return await db
                .FollowedCompanyRecords.AsNoTracking()
                .Where(x =>
                    x.Candidate.AccountId == accountId
                    && x.Status == EntityStatus.Active
                    && x.Company.Status == EntityStatus.Active
                )
                .Select(x => x.CompanyId)
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
                "CompanyDirectoryRepository.FollowedAsync"
            );
            throw;
        }
    }

    public async Task SetFollowedAsync(
        Guid accountId,
        Guid companyId,
        bool followed,
        CancellationToken ct
    )
    {
        try
        {
            if (!await Companies.AnyAsync(x => x.Id == companyId, ct))
                throw new NotFoundException("Company not found.");
            var candidateId =
                await db
                    .CandidateProfileRecords.Where(x =>
                        x.AccountId == accountId && x.Status == EntityStatus.Active
                    )
                    .Select(x => (Guid?)x.Id)
                    .SingleOrDefaultAsync(ct)
                ?? throw new NotFoundException("Candidate profile not found.");
            var record = await db.FollowedCompanyRecords.SingleOrDefaultAsync(
                x => x.CandidateId == candidateId && x.CompanyId == companyId,
                ct
            );
            if (record is null)
            {
                if (!followed)
                    return;
                record = new FollowedCompany { CandidateId = candidateId, CompanyId = companyId };
                db.FollowedCompanyRecords.Add(record);
            }

            record.Status = followed ? EntityStatus.Active : EntityStatus.Deleted;
            record.UpdatedDate = DateTimeOffset.UtcNow;
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                db.ChangeTracker.Clear();
                var existing = await db.FollowedCompanyRecords.SingleAsync(
                    x => x.CandidateId == candidateId && x.CompanyId == companyId,
                    ct
                );
                existing.Status = followed ? EntityStatus.Active : EntityStatus.Deleted;
                await db.SaveChangesAsync(ct);
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
                "CompanyDirectoryRepository.SetFollowedAsync"
            );
            throw;
        }
    }
}
