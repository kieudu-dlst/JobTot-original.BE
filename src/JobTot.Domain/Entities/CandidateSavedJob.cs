namespace JobTot.Domain.Entities;

public sealed class CandidateSavedJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public CandidateProfile Candidate { get; set; } = null!;
    public Guid JobId { get; set; }
    public JobPost Job { get; set; } = null!;
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
}
