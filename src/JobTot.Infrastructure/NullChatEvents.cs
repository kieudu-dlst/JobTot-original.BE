using JobTot.Application.Features;
namespace JobTot.Infrastructure;

public sealed class NullChatEvents : IChatEvents
{
    public void Publish(IEnumerable<Guid> accounts, ChatEvent notification)
    {
    }
}
