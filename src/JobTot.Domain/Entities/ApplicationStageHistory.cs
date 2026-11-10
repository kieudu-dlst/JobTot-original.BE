namespace JobTot.Domain.Entities;

public sealed class ApplicationStageHistory : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public JobApplication Application { get; set; } = null!;
    public Guid StageId { get; set; }
    public CampaignStage Stage { get; set; } = null!;
    public Guid ChangedByRecruiterId { get; set; }
    public RecruiterProfile ChangedByRecruiter { get; set; } = null!;
    public string? Note { get; set; }
}
