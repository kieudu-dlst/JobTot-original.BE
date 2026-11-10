namespace JobTot.Domain.Entities;

public sealed class TalentPoolTag : AuditableEntity
{
    public Guid TalentPoolEntryId { get; set; }
    public TalentPoolEntry TalentPoolEntry { get; set; } = null!;
    public Guid TalentTagId { get; set; }
    public TalentTag TalentTag { get; set; } = null!;
}
