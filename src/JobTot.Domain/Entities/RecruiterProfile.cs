namespace JobTot.Domain.Entities;

public sealed class RecruiterProfile : AuditableEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string? JobTitle { get; set; }
    public string? WorkPhone { get; set; }
    public bool IsCompanyAdmin { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public ICollection<RecruitmentCampaign> RecruitmentCampaigns { get; set; } =
        new List<RecruitmentCampaign>();
    public ICollection<ApplicationStageHistory> ApplicationStageHistorys { get; set; } =
        new List<ApplicationStageHistory>();
    public ICollection<InterviewFeedback> InterviewFeedbacks { get; set; } =
        new List<InterviewFeedback>();
    public ICollection<TalentPoolEntry> TalentPoolEntrys { get; set; } =
        new List<TalentPoolEntry>();
}
