namespace JobTot.Domain.Entities;

public sealed class TalentPoolEntry : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid CandidateId { get; set; }
    public CandidateProfile Candidate { get; set; } = null!;
    public Guid AddedByRecruiterId { get; set; }
    public RecruiterProfile AddedByRecruiter { get; set; } = null!;
    public string? ReadinessStatus { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public ICollection<TalentPoolTag> TalentPoolTags { get; set; } = new List<TalentPoolTag>();
}
