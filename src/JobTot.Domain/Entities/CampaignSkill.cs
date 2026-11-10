namespace JobTot.Domain.Entities;

public sealed class CampaignSkill : AuditableEntity
{
    public Guid CampaignId { get; set; }
    public RecruitmentCampaign Campaign { get; set; } = null!;
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public bool IsRequired { get; set; }
}
