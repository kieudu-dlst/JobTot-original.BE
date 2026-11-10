namespace JobTot.Domain.Entities;

public sealed class ArticleCategory : AuditableEntity
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
