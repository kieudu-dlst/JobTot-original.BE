using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api;

public sealed class EmailSecurityException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public sealed record EmailVerificationResult(string? Token, string? Error);

public interface IAccountEmailSender
{
    Task SendAsync(string email, string subject, string body, CancellationToken ct);
}

public sealed class SmtpAccountEmailSender(IConfiguration configuration) : IAccountEmailSender
{
    public async Task SendAsync(string email, string subject, string body, CancellationToken ct)
    {
        var settings = configuration.GetSection("PasswordRecovery:Smtp");
        if (
            string.IsNullOrWhiteSpace(settings["Host"])
            || string.IsNullOrWhiteSpace(settings["From"])
        )
            throw new EmailSecurityException(
                503,
                "Chưa cấu hình gửi email. Vui lòng liên hệ quản trị viên."
            );
        using var smtp = new SmtpClient(settings["Host"], settings.GetValue("Port", 587))
        {
            EnableSsl = settings.GetValue("EnableSsl", true),
            UseDefaultCredentials = false,
        };
        if (!string.IsNullOrWhiteSpace(settings["Username"]))
            smtp.Credentials = new NetworkCredential(settings["Username"], settings["Password"]);
        using var mail = new MailMessage(settings["From"]!, email)
        {
            Subject = subject,
            Body = body,
        };
        try
        {
            await smtp.SendMailAsync(mail, ct);
        }
        catch (SmtpException)
        {
            throw new EmailSecurityException(503, "Chưa gửi được email. Vui lòng thử lại sau.");
        }
    }
}

public sealed class AccountEmailSecurity(
    RecruitmentDbContext db,
    IAccountEmailSender sender,
    IAccountPasswordHasher passwords,
    IDataProtectionProvider protection
)
{
    private readonly IDataProtector protector = protection.CreateProtector(
        "JobTot.EmailSecurity.v1"
    );

    private sealed record Ticket(Guid ChallengeId, string Purpose, string? PasswordStamp = null);

    private static string Hash(Guid id, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id + ":" + code)));

    private static string Stamp(string? hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash ?? "")));

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private async Task Cooldown(string email, string role, string purpose, CancellationToken ct)
    {
        var since = DateTimeOffset.UtcNow.AddSeconds(-60);
        if (
            await db.AccountEmailChallenges.AnyAsync(
                x =>
                    x.Email == email
                    && x.Role == role
                    && x.Purpose == purpose
                    && x.CreatedAt > since,
                ct
            )
        )
            throw new EmailSecurityException(
                429,
                "Vui lòng đợi 60 giây trước khi yêu cầu email mới."
            );
    }

    public async Task<Guid> RequestVerification(string email, string role, CancellationToken ct)
    {
        email = Normalize(email);
        await Cooldown(email, role, "registration", ct);
        var challenge = new AccountEmailChallenge
        {
            Email = email,
            Role = role,
            Purpose = "registration",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
        };
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        challenge.SecretHash = Hash(challenge.Id, code);
        db.Add(challenge);
        await db.SaveChangesAsync(ct);
        try
        {
            await sender.SendAsync(
                email,
                "Mã xác thực email Job Tốt",
                $"Mã xác thực của bạn là: {code}\nMã có hiệu lực 10 phút. Không chia sẻ mã này cho người khác.",
                ct
            );
        }
        catch
        {
            db.Remove(challenge);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        return challenge.Id;
    }

    public async Task<EmailVerificationResult> Verify(
        Guid id,
        string email,
        string role,
        string code,
        CancellationToken ct
    )
    {
        var challenge = await db.AccountEmailChallenges.SingleOrDefaultAsync(
            x =>
                x.Id == id
                && x.Email == Normalize(email)
                && x.Role == role
                && x.Purpose == "registration",
            ct
        );
        if (
            challenge is null
            || challenge.ExpiresAt <= DateTimeOffset.UtcNow
            || challenge.ConsumedAt != null
            || challenge.Attempts >= 5
            || challenge.VerifiedAt != null
        )
            return new EmailVerificationResult(
                null,
                "Mã đã hết hạn hoặc không còn khả dụng. Hãy yêu cầu mã mới."
            );
        if (
            await db.AccountEmailChallenges.AnyAsync(
                x =>
                    x.Email == challenge.Email
                    && x.Role == role
                    && x.Purpose == "registration"
                    && x.CreatedAt > challenge.CreatedAt,
                ct
            )
        )
            return new EmailVerificationResult(null, "Hãy sử dụng mã mới nhất được gửi tới email.");
        challenge.Attempts++;
        challenge.Revision = Guid.NewGuid();
        var valid = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(challenge.SecretHash),
            Convert.FromHexString(Hash(id, code))
        );
        if (valid)
            challenge.VerifiedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        if (!valid)
            return new EmailVerificationResult(null, "Mã xác thực không đúng.");
        return new EmailVerificationResult(
            protector.Protect(JsonSerializer.Serialize(new Ticket(id, "registration"))),
            null
        );
    }

    private Ticket Decode(string? token, string purpose)
    {
        try
        {
            var ticket = JsonSerializer.Deserialize<Ticket>(protector.Unprotect(token ?? ""));
            if (ticket?.Purpose == purpose)
                return ticket;
        }
        catch (Exception ex)
            when (ex is CryptographicException or JsonException or ArgumentException) { }
        throw new EmailSecurityException(400, "Thông tin xác thực không hợp lệ hoặc đã hết hạn.");
    }

    public async Task RequireVerification(
        string email,
        string role,
        string? token,
        CancellationToken ct
    )
    {
        var ticket = Decode(token, "registration");
        var row = await db.AccountEmailChallenges.SingleOrDefaultAsync(
            x =>
                x.Id == ticket.ChallengeId
                && x.Email == Normalize(email)
                && x.Role == role
                && x.Purpose == "registration",
            ct
        );
        if (
            row is null
            || row.VerifiedAt == null
            || row.ConsumedAt != null
            || row.ExpiresAt <= DateTimeOffset.UtcNow
        )
            throw new EmailSecurityException(400, "Vui lòng xác thực email trước khi đăng ký.");
        row.ConsumedAt = DateTimeOffset.UtcNow;
        row.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
    }

    public async Task RequestTemporaryPassword(string email, string role, CancellationToken ct)
    {
        email = Normalize(email);
        await Cooldown(email, role, "temporary-password", ct);
        var account = await db.AccountRecords.SingleOrDefaultAsync(
            a =>
                a.Email == email
                && a.AccountType == role
                && a.AccountStatus == "Active"
                && a.Status == EntityStatus.Active,
            ct
        );
        if (account is null)
            return;
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)) + "a!";
        var row = new AccountEmailChallenge
        {
            Email = email,
            Role = role,
            Purpose = "temporary-password",
            AccountId = account.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            SecretHash = passwords.Hash(account, secret),
            PasswordStamp = Stamp(account.PasswordHash),
        };
        // Existing password stays valid until the emailed temporary password is exchanged.
        db.Add(row);
        await db.SaveChangesAsync(ct);
        try
        {
            await sender.SendAsync(
                email,
                "Mật khẩu tạm Job Tốt",
                $"Mật khẩu tạm của bạn: {secret}\nCó hiệu lực 30 phút. Đăng nhập bằng mật khẩu này để đặt mật khẩu mới.\nNếu không yêu cầu, hãy bỏ qua email; mật khẩu hiện tại chưa thay đổi.",
                ct
            );
        }
        catch
        {
            db.Remove(row);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<string?> TemporaryLogin(
        string email,
        string role,
        string password,
        CancellationToken ct
    )
    {
        var row = await db
            .AccountEmailChallenges.Where(x =>
                x.Email == Normalize(email) && x.Role == role && x.Purpose == "temporary-password"
            )
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
        if (
            row is null
            || row.ExpiresAt <= DateTimeOffset.UtcNow
            || row.ConsumedAt != null
            || row.Attempts >= 5
        )
            return null;
        var account = await db.AccountRecords.SingleOrDefaultAsync(
            x =>
                x.Id == row.AccountId
                && x.AccountType == role
                && x.AccountStatus == "Active"
                && x.Status == EntityStatus.Active,
            ct
        );
        if (account is null || row.PasswordStamp != Stamp(account.PasswordHash))
            return null;
        row.Attempts++;
        row.Revision = Guid.NewGuid();
        var valid = passwords.Verify(
            new Account { PasswordHash = row.SecretHash },
            password,
            out _
        );
        await db.SaveChangesAsync(ct);
        return valid
            ? protector.Protect(
                JsonSerializer.Serialize(
                    new Ticket(row.Id, "temporary-password", account.PasswordHash)
                )
            )
            : null;
    }

    public async Task ChangeTemporaryPassword(
        string role,
        string token,
        string password,
        CancellationToken ct
    )
    {
        var ticket = Decode(token, "temporary-password");
        var row = await db.AccountEmailChallenges.SingleOrDefaultAsync(
            x => x.Id == ticket.ChallengeId && x.Role == role && x.Purpose == "temporary-password",
            ct
        );
        if (row is null || row.ConsumedAt != null || row.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new EmailSecurityException(400, "Mật khẩu tạm đã hết hạn hoặc đã sử dụng.");
        var account = await db.AccountRecords.SingleAsync(x => x.Id == row.AccountId, ct);
        if (
            account.PasswordHash != ticket.PasswordStamp
            || account.Status != EntityStatus.Active
            || account.AccountStatus != "Active"
        )
            throw new EmailSecurityException(
                400,
                "Thông tin tài khoản đã thay đổi. Hãy yêu cầu lại."
            );
        account.PasswordHash = passwords.Hash(account, password);
        row.ConsumedAt = DateTimeOffset.UtcNow;
        row.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
    }
}
