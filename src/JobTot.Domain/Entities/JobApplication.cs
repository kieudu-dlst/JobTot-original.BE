namespace JobTot.Domain.Entities;

public sealed class JobApplication : AuditableEntity
{
    public Guid CampaignId { get; set; }
    public RecruitmentCampaign Campaign { get; set; } = null!;
    public Guid CandidateId { get; set; }
    public CandidateProfile Candidate { get; set; } = null!;
    public Guid? CvId { get; set; }
    public CandidateCv? Cv { get; set; }
    public Guid? StageId { get; set; }
    public CampaignStage? Stage { get; set; }
    public string? Source { get; set; }
    public string? ApplicationStatus { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public string? CoverLetter { get; set; }
    public decimal? MatchScore { get; set; }
    public ICollection<ApplicationStageHistory> ApplicationStageHistorys { get; set; } =
        new List<ApplicationStageHistory>();
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}
