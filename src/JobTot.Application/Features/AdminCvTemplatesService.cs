using JobTot.Application.Authentication;
using JobTot.Application.Cvs;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;

namespace JobTot.Application.Features;

public sealed class AdminCvTemplatesService(
    IRecruitmentData db,
    ICvRepository repository,
    IAccountPasswordHasher hasher,
    IAccessTokenIssuer issuer,
    ICurrentActor actor
) : ApplicationService(actor)
{
    public async Task<UseCaseResult> Login(CandidateLoginRequest input, CancellationToken ct)
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
                return Unauthenticated(
                    new FailureDetails { Detail = "Thông tin đăng nhập admin không hợp lệ." }
                );
            if (rehash)
                account.PasswordHash = hasher.Hash(account, input.Password);
            var expires = DateTimeOffset.UtcNow.AddHours(2);
            var session = new CandidateRefreshSession
            {
                AccountId = account.Id,
                PasswordStamp = PasswordStamp.Hash(account.PasswordHash!),
                ExpiresAt = expires,
                TokenHash = Guid.NewGuid().ToString(),
            };
            db.Add(session);
            await db.SaveChangesAsync(ct);
            return Success(
                new
                {
                    AccessToken = issuer.Issue(
"Admin",
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
    public async Task<UseCaseResult> Logout(CancellationToken ct)
    {
        try
        {
            var sid = Actor.SessionId;
            var session = await db.CandidateRefreshSessions.SingleAsync(s => s.Id == sid, ct);
            session.RevokedAt = DateTimeOffset.UtcNow;
            session.Version = Guid.NewGuid();
            await db.SaveChangesAsync(ct);
            return Completed();
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
    public async Task<UseCaseResult> List(CancellationToken ct)
    {
        try
        {
            return Success(await repository.TemplatesAsync(true, ct));
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
    public async Task<UseCaseResult> Get(Guid id, CancellationToken ct)
    {
        try
        {
            return await repository.TemplateAsync(id, true, ct) is { } t ? Success(t) : NotFound();
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
    public async Task<UseCaseResult> Create(CvTemplateWrite input, CancellationToken ct)
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
    public async Task<UseCaseResult> Update(Guid id, CvTemplateWrite input, CancellationToken ct)
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

    private async Task<UseCaseResult> Write(Guid? id, CvTemplateWrite input, CancellationToken ct)
    {
        try
        {
            try
            {
                return
                    await repository.WriteTemplateAsync(
                        id,
Actor.AccountId,
                        input,
                        ct
                    )
                        is { } t
                    ? Success(t)
                    : NotFound();
            }
            catch (CvVersionConflictException)
            {
                return Conflict(
                    new FailureDetails
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
