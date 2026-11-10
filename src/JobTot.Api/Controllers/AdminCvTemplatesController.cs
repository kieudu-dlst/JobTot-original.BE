using System.Security.Claims;
using JobTot.Application.Authentication;
using JobTot.Application.Cvs;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api.Controllers;

[
    ApiController,
    Route("api/admin"),
    ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)
]
public sealed class AdminCvTemplatesController(
    RecruitmentDbContext db,
    ICvRepository repository,
    IAccountPasswordHasher hasher,
    CandidateJwtSettings settings
) : ControllerBase
{
    [HttpPost("auth/login"), EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> Login(CandidateLoginRequest input, CancellationToken ct)
    {
        try
        {
            var login = input.EmailOrPhone.Trim().ToLowerInvariant();
            var account = await db.AccountRecords.SingleOrDefaultAsync(
                a => a.Email == login || a.Phone == login,
                ct
            );
            var valid = hasher.Verify(account ?? new Account(), input.Password, out var rehash);
            if (
                !valid
                || account is null
                || account.AccountType != "Admin"
                || account.AccountStatus != "Active"
                || account.Status != EntityStatus.Active
            )
                return Unauthorized(
                    new ProblemDetails { Detail = "Thông tin đăng nhập admin không hợp lệ." }
                );
            if (rehash)
                account.PasswordHash = hasher.Hash(account, input.Password);
            var expires = DateTimeOffset.UtcNow.AddHours(2);
            var session = new CandidateRefreshSession
            {
                AccountId = account.Id,
                PasswordStamp = AdminAuthentication.Stamp(account.PasswordHash!),
                ExpiresAt = expires,
                TokenHash = Guid.NewGuid().ToString(),
            };
            db.Add(session);
            await db.SaveChangesAsync(ct);
            return Ok(
                new
                {
                    AccessToken = AdminAuthentication.Issue(
                        settings,
                        account.Id,
                        session.Id,
                        account.FullName,
                        expires
                    ),
                    ExpiresAt = expires,
                    account.FullName,
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
                "AdminCvTemplatesController.Login"
            );
            throw;
        }
    }

    [HttpPost("auth/logout"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try
        {
            var sid = Guid.Parse(User.FindFirstValue("sid")!);
            var session = await db.CandidateRefreshSessions.SingleAsync(s => s.Id == sid, ct);
            session.RevokedAt = DateTimeOffset.UtcNow;
            session.Version = Guid.NewGuid();
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AdminCvTemplatesController.Logout"
            );
            throw;
        }
    }

    [HttpGet("cv-templates"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        try
        {
            return Ok(await repository.TemplatesAsync(true, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AdminCvTemplatesController.List"
            );
            throw;
        }
    }

    [HttpGet("cv-templates/{id:guid}"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        try
        {
            return await repository.TemplateAsync(id, true, ct) is { } t ? Ok(t) : NotFound();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AdminCvTemplatesController.Get"
            );
            throw;
        }
    }

    [HttpPost("cv-templates"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> Create(CvTemplateWrite input, CancellationToken ct)
    {
        try
        {
            return await (Write(null, input, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AdminCvTemplatesController.Create"
            );
            throw;
        }
    }

    [HttpPut("cv-templates/{id:guid}"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> Update(Guid id, CvTemplateWrite input, CancellationToken ct)
    {
        try
        {
            return await (Write(id, input, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "AdminCvTemplatesController.Update"
            );
            throw;
        }
    }

    private async Task<IActionResult> Write(Guid? id, CvTemplateWrite input, CancellationToken ct)
    {
        try
        {
            try
            {
                return
                    await repository.WriteTemplateAsync(
                        id,
                        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                        input,
                        ct
                    )
                        is { } t
                    ? Ok(t)
                    : NotFound();
            }
            catch (CvVersionConflictException)
            {
                return Conflict(
                    new ProblemDetails
                    {
                        Detail = "Mẫu đã thay đổi ở tab khác. Hãy tải lại trước khi lưu.",
                    }
                );
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
                "AdminCvTemplatesController.Write"
            );
            throw;
        }
    }
}
