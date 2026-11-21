namespace JobTot.Application.Execution;

public interface IAppConfiguration
{
    string? this[string key] { get; }
}
public interface IRuntimeEnvironment
{
    bool IsDevelopment
    {
        get;
    }
}
public interface ISecretProtector
{
    string Protect(string value); string Unprotect(string value);
}
public interface ISecretProtection
{
    ISecretProtector CreateProtector(string purpose);
}
public interface IOutboundHttpClients
{
    HttpClient CreateClient(string name);
}
public interface ITransientState
{
    bool TryGetValue<T>(object key, out T value);
    void Set<T>(object key, T value, DateTimeOffset expires);
    void Remove(object key);
}
