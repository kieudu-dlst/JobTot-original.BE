namespace JobTot.Domain.Entities;

public sealed class CampaignStage : AuditableEntity
{
    public Guid CampaignId { get; set; }
    public RecruitmentCampaign Campaign { get; set; } = null!;
    public string StageName { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public string? StageKind { get; set; }
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    public ICollection<ApplicationStageHistory> ApplicationStageHistorys { get; set; } =
        new List<ApplicationStageHistory>();
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}
