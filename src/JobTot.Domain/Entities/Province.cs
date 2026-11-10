namespace JobTot.Domain.Entities;

public sealed class Province : AuditableEntity
{
    public string ProvinceName { get; set; } = string.Empty;
    public string ProvinceCode { get; set; } = string.Empty;
    public ICollection<Company> Companys { get; set; } = new List<Company>();
    public ICollection<CandidateProfile> CandidateProfiles { get; set; } =
        new List<CandidateProfile>();
    public ICollection<RecruitmentCampaign> RecruitmentCampaigns { get; set; } =
        new List<RecruitmentCampaign>();
}
