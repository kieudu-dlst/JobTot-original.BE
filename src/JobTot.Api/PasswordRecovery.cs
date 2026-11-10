using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using JobTot.Application.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace JobTot.Api;

public interface IRecoveryEmailSender
{
    Task SendAsync(string email, string link, CancellationToken ct);
}

public sealed class SmtpRecoveryEmailSender(IConfiguration configuration) : IRecoveryEmailSender
{
    public async Task SendAsync(string email, string link, CancellationToken ct)
    {
        try
        {
            var settings = configuration.GetSection("PasswordRecovery:Smtp");
            var host =
                settings["Host"]
                ?? throw new InvalidOperationException("Password recovery SMTP is not configured.");
            var from =
                settings["From"]
                ?? throw new InvalidOperationException(
                    "Password recovery sender is not configured."
                );
            using var client = new SmtpClient(host, settings.GetValue("Port", 587))
            {
                EnableSsl = settings.GetValue("EnableSsl", true),
                UseDefaultCredentials = false,
            };
            if (!string.IsNullOrWhiteSpace(settings["Username"]))
                client.Credentials = new NetworkCredential(
                    settings["Username"],
                    settings["Password"]
                );
            using var message = new MailMessage(from, email)
            {
                Subject = "Khôi phục mật khẩu Job Tốt",
                Body =
                    $"Bạn đã yêu cầu đặt lại mật khẩu Job Tốt. Mở liên kết sau trong 30 phút:\n\n{link}\n\nNếu không gửi yêu cầu này, bạn có thể bỏ qua email.",
            };
            await client.SendMailAsync(message, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "SmtpRecoveryEmailSender.SendAsync"
            );
            throw;
        }
    }
}

public sealed class PasswordRecovery(
    ICandidateAuthRepository repository,
    IAccountPasswordHasher passwords,
    IDataProtectionProvider protection,
    IRecoveryEmailSender sender,
    IConfiguration configuration
)
{
    private readonly IDataProtector protector = protection.CreateProtector(
        "JobTot.Candidate.PasswordReset.v1"
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
                || !CandidateAuthService.IsActiveCandidate(account)
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
                $"{url!.TrimEnd('/')}/reset-password#token={Uri.EscapeDataString(token)}",
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
                "PasswordRecovery.RequestAsync"
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
                || !CandidateAuthService.IsActiveCandidate(account)
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
                "PasswordRecovery.ResetAsync"
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
                "PasswordRecovery.InvalidLink"
            );
            throw;
        }
    }
}
