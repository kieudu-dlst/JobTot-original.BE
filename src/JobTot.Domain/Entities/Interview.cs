namespace JobTot.Domain.Entities;

public sealed class Interview : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public JobApplication Application { get; set; } = null!;
    public Guid StageId { get; set; }
    public CampaignStage Stage { get; set; } = null!;
    public DateTimeOffset ScheduledStart { get; set; }
    public DateTimeOffset ScheduledEnd { get; set; }
    public string? MeetingUrl { get; set; }
    public string? Location { get; set; }
    public string? InterviewStatus { get; set; }
    public ICollection<InterviewFeedback> InterviewFeedbacks { get; set; } =
        new List<InterviewFeedback>();
}
