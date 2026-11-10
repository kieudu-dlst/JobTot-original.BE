namespace JobTot.Domain.Entities;

public sealed class SavedJob : AuditableEntity
{
    public Guid CandidateId { get; set; }
    public CandidateProfile Candidate { get; set; } = null!;
    public Guid RecruitmentCampaignId { get; set; }
    public RecruitmentCampaign RecruitmentCampaign { get; set; } = null!;
    public DateTimeOffset SavedAt { get; set; }
}
