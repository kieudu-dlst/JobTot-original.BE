using JobTot.Domain.Enums;

namespace JobTot.Application.Authentication;

public sealed class AdminProvisioning(IRecruitmentData data, IAccountPasswordHasher passwords)
{
    public async Task CreateAsync(string? email, string? password, CancellationToken ct)
    {
        email = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || password is null || password.Length < 12)
            throw new InvalidOperationException("Set JOBTOT_ADMIN_EMAIL and JOBTOT_ADMIN_PASSWORD (at least 12 characters).");
        if (await data.AccountRecords.AnyAsync(a => a.Email == email, ct))
            throw new InvalidOperationException("Account already exists; no account was modified.");
        var account = new Account { Email = email, FullName = "Quản trị viên", AccountType = "Admin", AccountStatus = "Active" };
        account.PasswordHash = passwords.Hash(account, password);
        data.Add(account);
        await data.SaveChangesAsync(ct);
    }
}

public sealed class AdminSessions(IRecruitmentData data)
{
    public async Task<bool> ValidateAsync(Guid accountId, Guid sessionId, CancellationToken ct)
    {
        var credentials = await (
            from s in data.CandidateRefreshSessions.ReadOnly()
            join a in data.AccountRecords.ReadOnly() on s.AccountId equals a.Id
            where s.Id == sessionId && a.Id == accountId && s.RevokedAt == null
                && s.ExpiresAt > DateTimeOffset.UtcNow && a.AccountType == "Admin"
                && a.AccountStatus == "Active" && a.Status == EntityStatus.Active
            select new
            {
                a.PasswordHash,
                s.PasswordStamp
            }
        ).SingleOrDefaultAsync(ct);
        return credentials?.PasswordHash is { } hash && credentials.PasswordStamp == PasswordStamp.Hash(hash);
    }
}
