namespace JobTot.Domain.Entities;

public sealed class CandidateRefreshSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string PasswordStamp { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}

public sealed class UsedCandidateRefreshToken
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
}
