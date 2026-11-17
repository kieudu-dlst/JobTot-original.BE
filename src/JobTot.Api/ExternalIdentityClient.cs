using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace JobTot.Api;

public sealed record ExternalIdentity(string Subject, string? Email, string? Name, bool EmailVerified);
public sealed record ExternalProvider(string Name, string ClientId, string ClientSecret,
    string AuthorizationEndpoint, string TokenEndpoint, string UserInfoEndpoint,
    string MetadataAddress, string Scope);

public interface IExternalIdentityClient
{
    ExternalProvider Provider(string provider, string role);
    Task<ExternalIdentity> Exchange(ExternalProvider provider, string code, string callback,
        string verifier, string nonce, CancellationToken ct);
}

public sealed class ExternalIdentityClient(IConfiguration configuration, IHttpClientFactory clients)
    : IExternalIdentityClient
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string,
        ConfigurationManager<OpenIdConnectConfiguration>> Discovery = new();

    public static string Role(string role) => role switch
    {
        "candidate" => "Candidate",
        "employer" => "Employer",
        _ => throw new EmailSecurityException(400, "Vai trò tài khoản không hợp lệ."),
    };

    public static string AllowedProvider(string provider, string role)
    {
        Role(role);
        if (!string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
            throw new EmailSecurityException(400, "Phương thức xác thực không được hỗ trợ. Vui lòng sử dụng email hoặc Google.");
        return "Google";
    }

    public ExternalProvider Provider(string provider, string role)
    {
        var name = AllowedProvider(provider, role);
        var section = configuration.GetSection($"ExternalAuth:{name}");
        var id = section["ClientId"];
        var secret = section["ClientSecret"];
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret))
            throw new EmailSecurityException(503, $"{name} chưa được cấu hình. Vui lòng sử dụng email hoặc thử lại sau.");
        return new(name, id, secret, "https://accounts.google.com/o/oauth2/v2/auth",
            "https://oauth2.googleapis.com/token", "https://openidconnect.googleapis.com/v1/userinfo",
            "https://accounts.google.com/.well-known/openid-configuration", "openid profile email");
    }

    public async Task<ExternalIdentity> Exchange(ExternalProvider provider, string code, string callback,
        string verifier, string nonce, CancellationToken ct)
    {
        var client = clients.CreateClient("external-auth");
        var fields = new Dictionary<string, string>
        {
            ["client_id"] = provider.ClientId,
            ["client_secret"] = provider.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = callback,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier,
        };
        using var tokenResponse = await client.PostAsync(provider.TokenEndpoint, new FormUrlEncodedContent(fields), ct);
        tokenResponse.EnsureSuccessStatusCode();
        using var tokens = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
        var accessToken = tokens.RootElement.GetProperty("access_token").GetString()
            ?? throw new SecurityTokenException("Missing access token");
        var manager = Discovery.GetOrAdd(provider.MetadataAddress, address =>
            new ConfigurationManager<OpenIdConnectConfiguration>(address,
                new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever(client) { RequireHttps = true }));
        async Task<System.Security.Claims.ClaimsPrincipal> Validate()
        {
            var metadata = await manager.GetConfigurationAsync(ct);
            var claims = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(
                tokens.RootElement.GetProperty("id_token").GetString(), new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = metadata.Issuer,
                    ValidateAudience = true,
                    ValidAudience = provider.ClientId,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = metadata.SigningKeys,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromSeconds(60),
                }, out var validated);
            if (validated is JwtSecurityToken jwt && jwt.Audiences.Count() > 1
                && claims.FindFirst("azp")?.Value != provider.ClientId)
                throw new SecurityTokenException("Missing authorized party for multiple audiences");
            return claims;
        }
        System.Security.Claims.ClaimsPrincipal claims;
        try
        {
            claims = await Validate();
        }
        catch (SecurityTokenSignatureKeyNotFoundException) { manager.RequestRefresh(); claims = await Validate(); }
        if (claims.FindFirst("nonce")?.Value != nonce)
            throw new SecurityTokenException("Invalid nonce");
        if (claims.FindFirst("azp") is { } azp && azp.Value != provider.ClientId)
            throw new SecurityTokenException("Invalid authorized party");
        var subject = claims.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subject))
            throw new SecurityTokenException("Missing subject");
        using var request = new HttpRequestMessage(HttpMethod.Get, provider.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var data = user.RootElement;
        string? Field(string key) => data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        var userSubject = Field("sub");
        if (string.IsNullOrEmpty(userSubject) || userSubject.Length > 256 || userSubject != subject)
            throw new SecurityTokenException("Invalid userinfo subject");
        var email = Field("email");
        var verified = data.TryGetProperty("email_verified", out var emailVerified) && emailVerified.ValueKind == JsonValueKind.True;
        // Google is not authoritative for a third-party email without a hosted-domain claim.
        if (email?.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) != true && Field("hd") is null)
            verified = false;
        return new(userSubject, email, Field("name"), verified);
    }
}
