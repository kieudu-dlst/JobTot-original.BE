namespace JobTot.Domain.Entities;

public sealed class RecruitmentCampaign : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid OwnerRecruiterId { get; set; }
    public RecruiterProfile OwnerRecruiter { get; set; } = null!;
    public Guid IndustryId { get; set; }
    public Industry Industry { get; set; } = null!;
    public Guid ProvinceId { get; set; }
    public Province Province { get; set; } = null!;
    public Guid EmploymentTypeId { get; set; }
    public EmploymentType EmploymentType { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? WorkAddress { get; set; }
    public string? WorkMode { get; set; }
    public string? Seniority { get; set; }
    public string? EducationLevel { get; set; }
    public int? ExperienceMinYears { get; set; }
    public int? ExperienceMaxYears { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public bool SalaryVisible { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    public string? GenderPreference { get; set; }
    public int Openings { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? CampaignStatus { get; set; }
    public string? Slug { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? PostingStatus { get; set; }
    public ICollection<CampaignSkill> CampaignSkills { get; set; } = new List<CampaignSkill>();
    public ICollection<CampaignBenefit> CampaignBenefits { get; set; } =
        new List<CampaignBenefit>();
    public ICollection<CampaignStage> CampaignStages { get; set; } = new List<CampaignStage>();
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
}
