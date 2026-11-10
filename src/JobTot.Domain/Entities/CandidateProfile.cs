namespace JobTot.Domain.Entities;

public sealed class CandidateProfile : AuditableEntity
{
    public string? DetailsJson { get; set; }
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public Guid? ProvinceId { get; set; }
    public Province? Province { get; set; }
    public string? Headline { get; set; }
    public DateOnly? BirthDate { get; set; }
    public int? YearsExperience { get; set; }
    public decimal? ExpectedSalaryMin { get; set; }
    public decimal? ExpectedSalaryMax { get; set; }
    public string? ReadyStatus { get; set; }
    public string? Summary { get; set; }
    public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
    public ICollection<CandidateCv> CandidateCvs { get; set; } = new List<CandidateCv>();
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    public ICollection<FollowedCompany> FollowedCompanys { get; set; } =
        new List<FollowedCompany>();
    public ICollection<TalentPoolEntry> TalentPoolEntrys { get; set; } =
        new List<TalentPoolEntry>();
}
