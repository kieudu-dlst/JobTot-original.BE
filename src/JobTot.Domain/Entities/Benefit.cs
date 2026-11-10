namespace JobTot.Domain.Entities;

public sealed class Benefit : AuditableEntity
{
    public string BenefitName { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public ICollection<CampaignBenefit> CampaignBenefits { get; set; } =
        new List<CampaignBenefit>();
}
