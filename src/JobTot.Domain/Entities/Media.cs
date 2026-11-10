namespace JobTot.Domain.Entities;

public sealed class Media : AuditableEntity
{
    public string Url { get; set; } = string.Empty;
    public string? Type { get; set; }
    public ICollection<Company> CompanyLogoFiles { get; set; } = new List<Company>();
    public ICollection<Company> CompanyCoverFiles { get; set; } = new List<Company>();
    public ICollection<CompanyMedia> CompanyMedias { get; set; } = new List<CompanyMedia>();
}
