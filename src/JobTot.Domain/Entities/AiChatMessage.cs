namespace JobTot.Domain.Entities;

public sealed class AiChatMessage
{
    public long Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid TurnId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";
    public string State { get; set; } = "Completed";
    public string? ImageName { get; set; }
    public string? ImageMimeType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
