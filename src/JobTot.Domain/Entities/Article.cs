namespace JobTot.Domain.Entities;

public sealed class Article : AuditableEntity
{
    public Guid ArticleCategoryId { get; set; }
    public ArticleCategory ArticleCategory { get; set; } = null!;
    public Guid AuthorAccountId { get; set; }
    public Account AuthorAccount { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public string? CoverUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
