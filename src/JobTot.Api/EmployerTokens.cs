using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JobTot.Api;

public sealed class EmployerTokens(
    RecruitmentDbContext db,
    EmployerAuthService accounts,
    CandidateJwtSettings settings
)
{
    public async Task<EmployerSessionDto> CreateAsync(
        EmployerAccountDto account,
        bool rememberMe,
        CancellationToken ct
    )
    {
        try
        {
            var session = new CandidateRefreshSession
            {
                AccountId = account.Id,
                PasswordStamp = (await accounts.GetPasswordStampAsync(account.Id, ct))!,
                ExpiresAt = DateTimeOffset.UtcNow.Add(
                    rememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(8)
                ),
            };
            var refreshToken = NewRefreshToken(session.Id);
            session.TokenHash = Hash(refreshToken);
            db.CandidateRefreshSessions.Add(session);
            await db.SaveChangesAsync(ct);
            return Issue(account, session, refreshToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerTokens.CreateAsync"
            );
            throw;
        }
    }

    public async Task<EmployerSessionDto?> RefreshAsync(string token, CancellationToken ct)
    {
        try
        {
            if (!TrySessionId(token, out var id))
                return null;
            var session = await db.CandidateRefreshSessions.SingleOrDefaultAsync(
                x => x.Id == id,
                ct
            );
            if (
                session is null
                || session.RevokedAt is not null
                || session.ExpiresAt <= DateTimeOffset.UtcNow
            )
                return null;
            if (
                !await db.AccountRecords.AnyAsync(
                    a =>
                        a.Id == session.AccountId
                        && a.AccountType == EmployerAuthService.EmployerRole,
                    ct
                )
            )
                return null;
            var hash = Hash(token);
            if (session.TokenHash != hash)
            {
                // A previously consumed token proves replay; revoke this whole login session.
                if (
                    await db.UsedCandidateRefreshTokens.AnyAsync(
                        x => x.SessionId == id && x.TokenHash == hash,
                        ct
                    )
                )
                    await RevokeAsync(id, ct);
                return null;
            }

            var account = await accounts.GetActiveAsync(session.AccountId, ct);
            if (
                account is null
                || session.PasswordStamp
                    != await accounts.GetPasswordStampAsync(session.AccountId, ct)
            )
            {
                await RevokeAsync(id, ct);
                return null;
            }

            var refreshToken = NewRefreshToken(id);
            db.UsedCandidateRefreshTokens.Add(
                new UsedCandidateRefreshToken { SessionId = id, TokenHash = hash }
            );
            session.TokenHash = Hash(refreshToken);
            session.Version = Guid.NewGuid();
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
                await RevokeAsync(id, ct);
                return null;
            }
            catch (DbUpdateException)
            {
                // An insert of the consumed hash may report a duplicate before the version check.
                db.ChangeTracker.Clear();
                if (
                    !await db.UsedCandidateRefreshTokens.AnyAsync(
                        x => x.SessionId == id && x.TokenHash == hash,
                        ct
                    )
                )
                    throw;
                await RevokeAsync(id, ct);
                return null;
            }

            return Issue(account, session, refreshToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerTokens.RefreshAsync"
            );
            throw;
        }
    }

    public async Task LogoutAsync(string token, CancellationToken ct)
    {
        try
        {
            if (!TrySessionId(token, out var id))
                return;
            if (
                !await db.CandidateRefreshSessions.AnyAsync(
                    s =>
                        s.Id == id
                        && db.AccountRecords.Any(a =>
                            a.Id == s.AccountId && a.AccountType == EmployerAuthService.EmployerRole
                        ),
                    ct
                )
            )
                return;
            var hash = Hash(token);
            if (
                await db.CandidateRefreshSessions.AnyAsync(
                    x => x.Id == id && x.TokenHash == hash,
                    ct
                )
                || await db.UsedCandidateRefreshTokens.AnyAsync(
                    x => x.SessionId == id && x.TokenHash == hash,
                    ct
                )
            )
                await RevokeAsync(id, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerTokens.LogoutAsync"
            );
            throw;
        }
    }

    public async Task<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        try
        {
            if (
                !Guid.TryParse(
                    principal.FindFirstValue(ClaimTypes.NameIdentifier),
                    out var accountId
                ) || !Guid.TryParse(principal.FindFirstValue("sid"), out var sessionId)
            )
                return false;
            var now = DateTimeOffset.UtcNow;
            // Authentication must not materialize the profile JSON (including the avatar).
            var credentials = await (
                from session in db.CandidateRefreshSessions.AsNoTracking()
                join account in db.AccountRecords.AsNoTracking()
                    on session.AccountId equals account.Id
                where
                    session.Id == sessionId
                    && account.Id == accountId
                    && session.RevokedAt == null
                    && session.ExpiresAt > now
                    && account.Status == EntityStatus.Active
                    && account.AccountType == EmployerAuthService.EmployerRole
                    && account.AccountStatus == EmployerAuthService.ActiveStatus
                    && account.RecruiterProfiles.Any(p =>
                        p.Status == EntityStatus.Active && p.Company.Status == EntityStatus.Active
                    )
                select new { session.PasswordStamp, account.PasswordHash }
            ).SingleOrDefaultAsync(ct);
            return credentials?.PasswordHash is { } hash
                && credentials.PasswordStamp
                    == Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerTokens.ValidateAsync"
            );
            throw;
        }
    }

    private async Task RevokeAsync(Guid id, CancellationToken ct)
    {
        try
        {
            // Retry optimistic concurrency so logout cannot lose a race with rotation.
            while (true)
            {
                db.ChangeTracker.Clear();
                var session = await db.CandidateRefreshSessions.SingleOrDefaultAsync(
                    x => x.Id == id,
                    ct
                );
                if (session is null || session.RevokedAt is not null)
                    return;
                session.RevokedAt = DateTimeOffset.UtcNow;
                session.Version = Guid.NewGuid();
                try
                {
                    await db.SaveChangesAsync(ct);
                    return;
                }
                catch (DbUpdateConcurrencyException)
                {
                    ct.ThrowIfCancellationRequested();
                }
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
                "EmployerTokens.RevokeAsync"
            );
            throw;
        }
    }

    private EmployerSessionDto Issue(
        EmployerAccountDto account,
        CandidateRefreshSession session,
        string refreshToken
    )
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(15);
            if (expiresAt > session.ExpiresAt)
                expiresAt = session.ExpiresAt;
            var jwt = new JwtSecurityToken(
                settings.Issuer,
                settings.Audience + ".Employer",
                [
                    new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                    new(ClaimTypes.Name, account.FullName),
                    new(ClaimTypes.Role, EmployerAuthService.EmployerRole),
                    new("sid", session.Id.ToString()),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                ],
                now.UtcDateTime,
                expiresAt.UtcDateTime,
                new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256)
            );
            return new(
                account,
                new JwtSecurityTokenHandler().WriteToken(jwt),
                refreshToken,
                expiresAt,
                session.ExpiresAt
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
                "EmployerTokens.Issue"
            );
            throw;
        }
    }

    private static string NewRefreshToken(Guid id)
    {
        try
        {
            return $"{id:N}.{Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48))}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerTokens.NewRefreshToken"
            );
            throw;
        }
    }

    private static string Hash(string token)
    {
        try
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerTokens.Hash"
            );
            throw;
        }
    }

    private static bool TrySessionId(string token, out Guid id)
    {
        try
        {
            id = default;
            return token.Length is > 33 and <= 256
                && token[32] == '.'
                && Guid.TryParseExact(token[..32], "N", out id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerTokens.TrySessionId"
            );
            throw;
        }
    }
}
