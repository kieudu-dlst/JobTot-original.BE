namespace JobTot.Domain.Entities;

public sealed class Account : AuditableEntity
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AccountType { get; set; }
    public string? AccountStatus { get; set; }
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public CandidateProfile? CandidateProfile { get; set; }
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
    public ICollection<RecruiterProfile> RecruiterProfiles { get; set; } =
        new List<RecruiterProfile>();
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
