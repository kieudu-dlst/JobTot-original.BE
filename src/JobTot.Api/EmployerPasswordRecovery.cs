using System.Security.Cryptography;
using System.Text.Json;
using JobTot.Application.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace JobTot.Api;

public sealed class EmployerPasswordRecovery(
    IEmployerAuthRepository repository,
    IAccountPasswordHasher passwords,
    IDataProtectionProvider protection,
    IRecoveryEmailSender sender,
    IConfiguration configuration
)
{
    private readonly IDataProtector protector = protection.CreateProtector(
        "JobTot.Employer.PasswordReset.v1"
    );

    private sealed record ResetTicket(
        Guid AccountId,
        string PasswordHash,
        DateTimeOffset ExpiresAt
    );

    public async Task RequestAsync(string email, CancellationToken ct)
    {
        try
        {
            var url = configuration["PasswordRecovery:FrontendUrl"];
            var smtpHost = configuration["PasswordRecovery:Smtp:Host"];
            // Fail consistently for every email when delivery has not been configured.
            if (
                !Uri.TryCreate(url, UriKind.Absolute, out var frontend)
                || frontend.Scheme is not ("https" or "http")
                || string.IsNullOrWhiteSpace(smtpHost)
                || string.IsNullOrWhiteSpace(configuration["PasswordRecovery:Smtp:From"])
            )
                throw new InvalidOperationException("Password recovery is not configured.");
            var account = await repository.FindAsync(
                new CandidateIdentifier(email.Trim().ToLowerInvariant(), null),
                ct
            );
            if (
                account is null
                || !EmployerAuthService.IsActiveEmployer(account)
                || account.PasswordHash is null
            )
                return;
            var token = protector.Protect(
                JsonSerializer.Serialize(
                    new ResetTicket(
                        account.Id,
                        account.PasswordHash,
                        DateTimeOffset.UtcNow.AddMinutes(30)
                    )
                )
            );
            await sender.SendAsync(
                account.Email!,
                $"{url!.TrimEnd('/')}/reset-password?role=employer#token={Uri.EscapeDataString(token)}",
                ct
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
                "EmployerPasswordRecovery.RequestAsync"
            );
            throw;
        }
    }

    public async Task ResetAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            ResetTicket? ticket;
            try
            {
                ticket = JsonSerializer.Deserialize<ResetTicket>(
                    protector.Unprotect(request.Token)
                );
            }
            catch (Exception failure)
                when (failure is CryptographicException or JsonException or ArgumentException)
            {
                throw InvalidLink();
            }

            if (ticket is null || ticket.ExpiresAt <= DateTimeOffset.UtcNow)
                throw InvalidLink();
            var account = await repository.GetAsync(ticket.AccountId, ct);
            if (
                account is null
                || !EmployerAuthService.IsActiveEmployer(account)
                || account.PasswordHash != ticket.PasswordHash
            )
                throw InvalidLink();
            if (
                !await repository.ResetPasswordAsync(
                    account.Id,
                    ticket.PasswordHash,
                    passwords.Hash(account, request.Password),
                    ct
                )
            )
                throw InvalidLink();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerPasswordRecovery.ResetAsync"
            );
            throw;
        }
    }

    private static ArgumentException InvalidLink()
    {
        try
        {
            return new(
                "Liên kết khôi phục không hợp lệ hoặc đã hết hạn. Vui lòng gửi yêu cầu mới."
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
                "EmployerPasswordRecovery.InvalidLink"
            );
            throw;
        }
    }
}
