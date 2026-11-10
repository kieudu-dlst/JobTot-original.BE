using System.Security.Claims;
using JobTot.Application;
using JobTot.Application.Administration;
using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/admin"), Authorize(Policy = AdminAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminManagementController(
    RecruitmentDbContext db,
    IAccountPasswordHasher passwords
) : ControllerBase
{
    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("auth/me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        try
        {
            return Ok(
                await db
                    .AccountRecords.AsNoTracking()
                    .Where(a => a.Id == AdminId)
                    .Select(a => new
                    {
                        a.Id,
                        a.FullName,
                        a.Email,
                        a.AccountType,
                    })
                    .SingleAsync(ct)
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.Me");
            throw;
        }
    }

    [HttpGet("companies")]
    public async Task<ActionResult<AdminPage<AdminCompanyRow>>> Companies(
        [FromQuery] AdminListQuery input,
        CancellationToken ct
    )
    {
        try
        {
            var query = db.Companies.AsNoTracking().Where(c => c.Status != EntityStatus.Deleted);
            if (input.Status is { } status)
                query = query.Where(c =>
                    c.Status == (status == "Active" ? EntityStatus.Active : EntityStatus.Inactive)
                );
            if (!string.IsNullOrWhiteSpace(input.Keyword))
            {
                var keyword = input.Keyword.Trim();
                query = query.Where(c =>
                    c.Name.Contains(keyword)
                    || (c.LegalName != null && c.LegalName.Contains(keyword))
                    || (c.TaxCode != null && c.TaxCode.Contains(keyword))
                    || c.Email.Contains(keyword)
                );
            }
            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(c => c.CreatedDate)
                .ThenBy(c => c.Id)
                .Skip((input.Page - 1) * input.Size)
                .Take(input.Size)
                .Select(c => new AdminCompanyRow(
                    c.Id,
                    c.Name,
                    c.LegalName,
                    c.TaxCode,
                    c.Email,
                    c.Phone,
                    c.WebsiteUrl,
                    c.AddressLine,
                    c.Industry == null ? null : c.Industry.IndustryName,
                    c.VerificationStatus ?? "Pending",
                    c.Status == EntityStatus.Active ? "Active" : "Inactive",
                    c.CreatedDate
                ))
                .ToListAsync(ct);
            return Ok(new AdminPage<AdminCompanyRow>(items, total, input.Page, input.Size));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.Companies");
            throw;
        }
    }

    [HttpPut("companies/{id:guid}/verification")]
    public async Task<IActionResult> VerifyCompany(
        Guid id,
        AdminVerificationInput input,
        CancellationToken ct
    )
    {
        try
        {
            var company = await db.Companies.SingleOrDefaultAsync(
                c => c.Id == id && c.Status != EntityStatus.Deleted,
                ct
            );
            if (company is null)
                return NotFound();
            if (
                input.VerificationStatus == "Verified"
                && (
                    string.IsNullOrWhiteSpace(company.LegalName)
                    || string.IsNullOrWhiteSpace(company.TaxCode)
                    || string.IsNullOrWhiteSpace(company.Email)
                    || string.IsNullOrWhiteSpace(company.AddressLine)
                )
            )
                return BadRequest(
                    new ProblemDetails
                    {
                        Detail =
                            "Cần có tên pháp lý, mã số thuế, email và địa chỉ trước khi duyệt công ty.",
                    }
                );
            company.VerificationStatus = input.VerificationStatus;
            Touch(company);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.VerifyCompany");
            throw;
        }
    }

    [HttpPut("companies/{id:guid}/status")]
    public async Task<IActionResult> CompanyStatus(
        Guid id,
        AdminStatusInput input,
        CancellationToken ct
    )
    {
        try
        {
            var company = await db.Companies.SingleOrDefaultAsync(
                c => c.Id == id && c.Status != EntityStatus.Deleted,
                ct
            );
            if (company is null)
                return NotFound();
            company.Status = input.Active == true ? EntityStatus.Active : EntityStatus.Inactive;
            Touch(company);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.CompanyStatus");
            throw;
        }
    }

    [HttpGet("users")]
    public async Task<ActionResult<AdminPage<AdminUserRow>>> Users(
        [FromQuery] AdminListQuery input,
        CancellationToken ct
    )
    {
        try
        {
            var query = db
                .AccountRecords.AsNoTracking()
                .Where(a => a.Status != EntityStatus.Deleted);
            if (input.Status == "Active")
                query = query.Where(a =>
                    a.Status == EntityStatus.Active && a.AccountStatus == "Active"
                );
            if (input.Status == "Inactive")
                query = query.Where(a =>
                    a.Status != EntityStatus.Active || a.AccountStatus != "Active"
                );
            if (!string.IsNullOrWhiteSpace(input.Keyword))
            {
                var keyword = input.Keyword.Trim();
                query = query.Where(a =>
                    a.FullName.Contains(keyword)
                    || (a.Email != null && a.Email.Contains(keyword))
                    || (a.Phone != null && a.Phone.Contains(keyword))
                );
            }
            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(a => a.CreatedDate)
                .ThenBy(a => a.Id)
                .Skip((input.Page - 1) * input.Size)
                .Take(input.Size)
                .Select(a => new AdminUserRow(
                    a.Id,
                    a.FullName,
                    a.Email,
                    a.Phone,
                    a.AccountType,
                    a.Status == EntityStatus.Active && a.AccountStatus == "Active"
                        ? "Active"
                        : "Inactive",
                    a.CreatedDate
                ))
                .ToListAsync(ct);
            return Ok(new AdminPage<AdminUserRow>(items, total, input.Page, input.Size));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.Users");
            throw;
        }
    }

    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UserStatus(
        Guid id,
        AdminStatusInput input,
        CancellationToken ct
    )
    {
        try
        {
            if (id == AdminId && input.Active == false)
                return BadRequest(
                    new ProblemDetails
                    {
                        Detail = "Không thể ngừng hoạt động tài khoản admin đang đăng nhập.",
                    }
                );
            var account = await db.AccountRecords.SingleOrDefaultAsync(
                a => a.Id == id && a.Status != EntityStatus.Deleted,
                ct
            );
            if (account is null)
                return NotFound();
            account.Status = input.Active == true ? EntityStatus.Active : EntityStatus.Inactive;
            account.AccountStatus = input.Active == true ? "Active" : "Inactive";
            Touch(account);
            if (input.Active == false)
                await RevokeSessions(id, ct);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.UserStatus");
            throw;
        }
    }

    [HttpPut("users/{id:guid}/password")]
    public async Task<IActionResult> Password(
        Guid id,
        AdminPasswordInput input,
        CancellationToken ct
    )
    {
        try
        {
            var account = await db.AccountRecords.SingleOrDefaultAsync(
                a => a.Id == id && a.Status != EntityStatus.Deleted,
                ct
            );
            if (account is null)
                return NotFound();
            account.PasswordHash = passwords.Hash(account, input.Password);
            Touch(account);
            await RevokeSessions(id, ct);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.Password");
            throw;
        }
    }

    [HttpGet("industries")]
    public async Task<ActionResult<AdminPage<AdminIndustryRow>>> Industries(
        [FromQuery] AdminListQuery input,
        CancellationToken ct
    )
    {
        try
        {
            var query = db
                .IndustryRecords.AsNoTracking()
                .Where(i => i.Status != EntityStatus.Deleted);
            if (input.Status is { } status)
                query = query.Where(i =>
                    i.Status == (status == "Active" ? EntityStatus.Active : EntityStatus.Inactive)
                );
            if (!string.IsNullOrWhiteSpace(input.Keyword))
                query = query.Where(i => i.IndustryName.Contains(input.Keyword.Trim()));
            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(i => i.IndustryName)
                .ThenBy(i => i.Id)
                .Skip((input.Page - 1) * input.Size)
                .Take(input.Size)
                .Select(i => new AdminIndustryRow(
                    i.Id,
                    i.IndustryName,
                    i.Status == EntityStatus.Active ? "Active" : "Inactive",
                    db.Companies.Count(c =>
                        c.Status != EntityStatus.Deleted
                        && (
                            c.IndustryId == i.Id
                            || c.CompanyIndustrys.Any(ci =>
                                ci.IndustryId == i.Id && ci.Status == EntityStatus.Active
                            )
                        )
                    )
                ))
                .ToListAsync(ct);
            return Ok(new AdminPage<AdminIndustryRow>(items, total, input.Page, input.Size));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.Industries");
            throw;
        }
    }

    [HttpPost("industries")]
    public async Task<IActionResult> CreateIndustry(AdminIndustryInput input, CancellationToken ct)
    {
        try
        {
            var name = input.Name.Trim();
            if (name.Length == 0)
                return BadRequest(
                    new ProblemDetails { Detail = "Tên nhóm ngành không được để trống." }
                );
            if (
                await db.IndustryRecords.AnyAsync(
                    i =>
                        i.Status != EntityStatus.Deleted
                        && i.IndustryName.ToLower() == name.ToLower(),
                    ct
                )
            )
                return Conflict(new ProblemDetails { Detail = "Tên nhóm ngành đã tồn tại." });
            var industry = new Industry { IndustryName = name, CreatedAccountId = AdminId };
            db.Add(industry);
            await db.SaveChangesAsync(ct);
            return Created(
                $"/api/admin/industries/{industry.Id}",
                new { industry.Id, Name = industry.IndustryName }
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.CreateIndustry");
            throw;
        }
    }

    [HttpPut("industries/{id:guid}")]
    public async Task<IActionResult> UpdateIndustry(
        Guid id,
        AdminIndustryInput input,
        CancellationToken ct
    )
    {
        try
        {
            var industry = await db.IndustryRecords.SingleOrDefaultAsync(
                i => i.Id == id && i.Status != EntityStatus.Deleted,
                ct
            );
            if (industry is null)
                return NotFound();
            var name = input.Name.Trim();
            if (name.Length == 0)
                return BadRequest(
                    new ProblemDetails { Detail = "Tên nhóm ngành không được để trống." }
                );
            if (
                await db.IndustryRecords.AnyAsync(
                    i =>
                        i.Id != id
                        && i.Status != EntityStatus.Deleted
                        && i.IndustryName.ToLower() == name.ToLower(),
                    ct
                )
            )
                return Conflict(new ProblemDetails { Detail = "Tên nhóm ngành đã tồn tại." });
            industry.IndustryName = name;
            Touch(industry);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.UpdateIndustry");
            throw;
        }
    }

    [HttpPut("industries/{id:guid}/status")]
    public async Task<IActionResult> IndustryStatus(
        Guid id,
        AdminStatusInput input,
        CancellationToken ct
    )
    {
        try
        {
            var industry = await db.IndustryRecords.SingleOrDefaultAsync(
                i => i.Id == id && i.Status != EntityStatus.Deleted,
                ct
            );
            if (industry is null)
                return NotFound();
            industry.Status = input.Active == true ? EntityStatus.Active : EntityStatus.Inactive;
            Touch(industry);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.IndustryStatus");
            throw;
        }
    }

    private void Touch(AuditableEntity entity)
    {
        try
        {
            entity.UpdatedAccountId = AdminId;
            entity.UpdatedDate = DateTimeOffset.UtcNow;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.Touch");
            throw;
        }
    }

    private async Task RevokeSessions(Guid accountId, CancellationToken ct)
    {
        try
        {
            var sessions = await db
                .CandidateRefreshSessions.Where(s =>
                    s.AccountId == accountId && s.RevokedAt == null
                )
                .ToListAsync(ct);
            foreach (var session in sessions)
            {
                session.RevokedAt = DateTimeOffset.UtcNow;
                session.Version = Guid.NewGuid();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionDiagnostics.Record(exception, "AdminManagementController.RevokeSessions");
            throw;
        }
    }
}
