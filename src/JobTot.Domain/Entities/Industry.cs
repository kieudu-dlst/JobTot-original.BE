namespace JobTot.Domain.Entities;

public sealed class Industry : AuditableEntity
{
    public string IndustryName { get; set; } = string.Empty;
    public ICollection<Company> Companys { get; set; } = new List<Company>();
    public ICollection<RecruitmentCampaign> RecruitmentCampaigns { get; set; } =
        new List<RecruitmentCampaign>();
    public ICollection<CompanyIndustry> CompanyIndustrys { get; set; } =
        new List<CompanyIndustry>();
}
