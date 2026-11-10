namespace JobTot.Domain.Entities;

public sealed class CompanyMedia : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid MediaId { get; set; }
    public Media Media { get; set; } = null!;
}
