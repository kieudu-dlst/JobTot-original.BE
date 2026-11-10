namespace JobTot.Domain.Entities;

/// <summary>A company's interest in a candidate, independent of its saved talent pool.</summary>
public sealed class EmployerCandidateInterest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid CandidateId { get; set; }
    public CandidateProfile Candidate { get; set; } = null!;
    public Guid InterestedByRecruiterId { get; set; }
    public RecruiterProfile InterestedByRecruiter { get; set; } = null!;
    public DateTimeOffset InterestedAt { get; set; } = DateTimeOffset.UtcNow;
}
