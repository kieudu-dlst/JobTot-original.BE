namespace JobTot.Domain.Entities;

public sealed class AccountEmailChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string Purpose { get; set; } = "";
    public Guid? AccountId { get; set; }
    public string SecretHash { get; set; } = "";
    public string? PasswordStamp { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int Attempts { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}
