using JobTot.Domain.Entities;

namespace JobTot.Infrastructure.Persistence;

public sealed class ChatConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PairKey { get; set; } = "";
    public Guid FirstAccountId { get; set; }
    public Account FirstAccount { get; set; } = null!;
    public Guid SecondAccountId { get; set; }
    public Account SecondAccount { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public sealed class ChatMessage
{
    public long Id { get; set; }
    public Guid ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public Guid SenderAccountId { get; set; }
    public Account SenderAccount { get; set; } = null!;
    public Guid ClientMessageId { get; set; }
    public string Text { get; set; } = "";
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}
