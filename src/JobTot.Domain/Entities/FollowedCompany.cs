namespace JobTot.Domain.Entities;

public sealed class FollowedCompany : AuditableEntity
{
    public Guid CandidateId { get; set; }
    public CandidateProfile Candidate { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
}
