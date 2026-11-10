namespace JobTot.Domain.Entities;

public sealed class CvCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<CvTemplate> CvTemplates { get; set; } = new List<CvTemplate>();
}
