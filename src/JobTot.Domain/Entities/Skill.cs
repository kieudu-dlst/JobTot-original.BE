namespace JobTot.Domain.Entities;

public sealed class Skill : AuditableEntity
{
    public string SkillName { get; set; } = string.Empty;
    public ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
    public ICollection<CampaignSkill> CampaignSkills { get; set; } = new List<CampaignSkill>();
}
