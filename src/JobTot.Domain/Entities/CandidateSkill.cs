namespace JobTot.Domain.Entities;

public sealed class CandidateSkill : AuditableEntity
{
    public Guid CandidateId { get; set; }
    public CandidateProfile Candidate { get; set; } = null!;
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public int? Proficiency { get; set; }
}
