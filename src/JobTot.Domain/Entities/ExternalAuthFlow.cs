namespace JobTot.Domain.Entities;

// Short-lived, browser-bound OAuth transaction. Provider tokens are never persisted.
public sealed class ExternalAuthFlow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BrowserSecretHash { get; set; } = "";
    public string ProtectedData { get; set; } = "";
    public DateTimeOffset ExpiresAt
    {
        get; set;
    }
    public Guid Revision { get; set; } = Guid.NewGuid();
}
