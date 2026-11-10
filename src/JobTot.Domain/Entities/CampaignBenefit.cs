namespace JobTot.Domain.Entities;

public sealed class CampaignBenefit : AuditableEntity
{
    public Guid CampaignId { get; set; }
    public RecruitmentCampaign Campaign { get; set; } = null!;
    public Guid BenefitId { get; set; }
    public Benefit Benefit { get; set; } = null!;
}
