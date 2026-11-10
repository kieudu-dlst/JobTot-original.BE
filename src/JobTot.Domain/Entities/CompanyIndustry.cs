namespace JobTot.Domain.Entities;

public sealed class CompanyIndustry : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid IndustryId { get; set; }
    public Industry Industry { get; set; } = null!;
}
