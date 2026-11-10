namespace JobTot.Domain.Entities;

public sealed class EmploymentType : AuditableEntity
{
    public string TypeName { get; set; } = string.Empty;
    public ICollection<RecruitmentCampaign> RecruitmentCampaigns { get; set; } =
        new List<RecruitmentCampaign>();
}
