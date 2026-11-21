using JobTot.Application.Execution;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace JobTot.Infrastructure;

public sealed class AppConfiguration(IConfiguration configuration) : IAppConfiguration
{
    public string? this[string key] => configuration[key];
}
public sealed class RuntimeEnvironment(IWebHostEnvironment environment) : IRuntimeEnvironment
{
    public bool IsDevelopment => environment.IsDevelopment();
}
public sealed class SecretProtection(IDataProtectionProvider provider) : ISecretProtection
{
    public ISecretProtector CreateProtector(string purpose) => new Protector(provider.CreateProtector(purpose));
    private sealed class Protector(IDataProtector protector) : ISecretProtector
    {
        public string Protect(string value) => protector.Protect(value);
        public string Unprotect(string value) => protector.Unprotect(value);
    }
}
public sealed class OutboundHttpClients(IHttpClientFactory clients) : IOutboundHttpClients
{
    public HttpClient CreateClient(string name) => clients.CreateClient(name);
}
public sealed class TransientState(IMemoryCache cache) : ITransientState
{
    public bool TryGetValue<T>(object key, out T value) => cache.TryGetValue(key, out value!);
    public void Set<T>(object key, T value, DateTimeOffset expires) => cache.Set(key, value, expires);
    public void Remove(object key) => cache.Remove(key);
}
