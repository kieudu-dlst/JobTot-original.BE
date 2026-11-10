namespace JobTot.Domain.Entities;

// Jobs and recruitment campaigns have independent identifiers; candidate submissions reference Jobs directly.
public sealed class CandidateJobSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public JobPost Job { get; set; } = null!;
    public Guid CandidateId { get; set; }
    public CandidateProfile Candidate { get; set; } = null!;
    public Guid CvId { get; set; }
    public CandidateCv Cv { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? CoverLetter { get; set; }
    public string CvTitle { get; set; } = "";
    public string CvContentJson { get; set; } = "";
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public string Status { get; set; } = "submitted";
}
