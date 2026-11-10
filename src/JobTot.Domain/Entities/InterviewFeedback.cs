namespace JobTot.Domain.Entities;

public sealed class InterviewFeedback : AuditableEntity
{
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;
    public Guid RecruiterId { get; set; }
    public RecruiterProfile Recruiter { get; set; } = null!;
    public decimal? Score { get; set; }
    public string? Decision { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
}
