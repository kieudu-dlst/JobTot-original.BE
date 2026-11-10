namespace JobTot.Domain.Entities;

public sealed class CareerArticle
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Content { get; set; } = "";
    public string? CoverUrl { get; set; }
    public string? CoverFileName { get; set; }
    public string? CoverContentType { get; set; }
    public long? CoverSize { get; set; }
    public bool Published { get; set; }
    public Guid Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
