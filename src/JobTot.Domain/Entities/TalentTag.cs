namespace JobTot.Domain.Entities;

public sealed class TalentTag : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string TagName { get; set; } = string.Empty;
    public ICollection<TalentPoolTag> TalentPoolTags { get; set; } = new List<TalentPoolTag>();
}
