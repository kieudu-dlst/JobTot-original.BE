namespace JobTot.Application.Features;

public sealed record ChatEvent(string Type, Guid ConversationId, Guid AccountId, object? Data = null);
public interface IChatEvents
{
    void Publish(IEnumerable<Guid> accounts, ChatEvent notification);
}
