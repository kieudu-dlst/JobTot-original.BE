namespace JobTot.Infrastructure.Persistence;

public sealed class AccountNotificationRead
{
    public Guid AccountId { get; set; }
    public string NotificationId { get; set; } = "";
    public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccountMessageRead
{
    public Guid AccountId { get; set; }
    public long MessageId { get; set; }
    public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;
}
