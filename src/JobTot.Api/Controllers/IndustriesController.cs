using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/industries")]
public sealed class IndustriesController(RecruitmentDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var jobs = db
                .Jobs.AsNoTracking()
                .Where(j =>
                    !j.IsClosed && j.ExpiresAt > now && j.Company.Status == EntityStatus.Active
                );
            var memberships = db
                .Companies.Where(c => c.Status == EntityStatus.Active && c.IndustryId != null)
                .Select(c => new { CompanyId = c.Id, IndustryId = c.IndustryId!.Value })
                .Concat(
                    db.CompanyIndustryRecords.Where(c => c.Status == EntityStatus.Active)
                        .Select(c => new { c.CompanyId, c.IndustryId })
                )
                .Distinct();
            var market =
                from j in jobs
                join m in memberships on j.CompanyId equals m.CompanyId
                select new
                {
                    m.IndustryId,
                    j.SalaryMin,
                    j.SalaryMax,
                    j.SalaryPeriod,
                    j.CreatedAt,
                };
            var counts = await market
                .GroupBy(j => j.IndustryId)
                .Select(g => new
                {
                    Id = g.Key,
                    OpenJobs = g.Count(),
                    NewJobs = g.Count(j => j.CreatedAt >= now.AddDays(-30)),
                    SalaryMin = g.Min(j =>
                        j.SalaryPeriod == "monthly" ? j.SalaryMin ?? j.SalaryMax : null
                    ),
                    SalaryMax = g.Max(j =>
                        j.SalaryPeriod == "monthly" ? j.SalaryMax ?? j.SalaryMin : null
                    ),
                })
                .ToListAsync(ct);
            var industries = await db
                .IndustryRecords.AsNoTracking()
                .Where(i => i.Status == EntityStatus.Active)
                .OrderBy(i => i.IndustryName)
                .Select(i => new { i.Id, Name = i.IndustryName })
                .ToListAsync(ct);
            var byId = counts.ToDictionary(i => i.Id);
            return Ok(
                industries
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        OpenJobs = byId.GetValueOrDefault(i.Id)?.OpenJobs ?? 0,
                        NewJobs = byId.GetValueOrDefault(i.Id)?.NewJobs ?? 0,
                        SalaryMin = byId.GetValueOrDefault(i.Id)?.SalaryMin,
                        SalaryMax = byId.GetValueOrDefault(i.Id)?.SalaryMax,
                    })
                    .OrderByDescending(i => i.OpenJobs)
                    .ThenBy(i => i.Name)
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
                "IndustriesController.Overview"
            );
            throw;
        }
    }
}
