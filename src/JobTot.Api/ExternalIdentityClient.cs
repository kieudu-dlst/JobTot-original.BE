using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace JobTot.Api;

public sealed record ExternalIdentity(string Subject, string? Email, string? Name, bool EmailVerified);
public sealed record ExternalProvider(string Name, string ClientId, string ClientSecret,
    string AuthorizationEndpoint, string TokenEndpoint, string UserInfoEndpoint,
    string? MetadataAddress, string Scope);

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
        var name = provider.ToLowerInvariant() switch
        {
            "google" => "Google",
            "facebook" => "Facebook",
            "linkedin" => "LinkedIn",
            _ => throw new EmailSecurityException(400, "Phương thức xác thực không được hỗ trợ."),
        };
        if (role == "employer" && name != "Google")
            throw new EmailSecurityException(403, "Nhà tuyển dụng chỉ được sử dụng email hoặc Google.");
        return name;
    }

    public ExternalProvider Provider(string provider, string role)
    {
        var name = AllowedProvider(provider, role);
        var section = configuration.GetSection($"ExternalAuth:{name}");
        var id = section["ClientId"];
        var secret = section["ClientSecret"];
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret))
            throw new EmailSecurityException(503, $"{name} chưa được cấu hình. Vui lòng sử dụng email hoặc thử lại sau.");
        var version = section["GraphApiVersion"];
        if (name == "Facebook" && (version is null || !System.Text.RegularExpressions.Regex.IsMatch(version, @"^v\d+\.\d+$")))
            throw new EmailSecurityException(503, "Facebook chưa được cấu hình phiên bản Graph API.");
        return name switch
        {
            "Google" => new(name, id, secret, "https://accounts.google.com/o/oauth2/v2/auth",
                "https://oauth2.googleapis.com/token", "https://openidconnect.googleapis.com/v1/userinfo",
                "https://accounts.google.com/.well-known/openid-configuration", "openid profile email"),
            "LinkedIn" => new(name, id, secret, "https://www.linkedin.com/oauth/v2/authorization",
                "https://www.linkedin.com/oauth/v2/accessToken", "https://api.linkedin.com/v2/userinfo",
                "https://www.linkedin.com/oauth/.well-known/openid-configuration", "openid profile email"),
            _ => new(name, id, secret, $"https://www.facebook.com/{version}/dialog/oauth",
                $"https://graph.facebook.com/{version}/oauth/access_token",
                $"https://graph.facebook.com/{version}/me", null, "public_profile,email"),
        };
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
        };
        if (provider.Name == "Google")
            fields["code_verifier"] = verifier;
        using var tokenResponse = await client.PostAsync(provider.TokenEndpoint, new FormUrlEncodedContent(fields), ct);
        tokenResponse.EnsureSuccessStatusCode();
        using var tokens = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
        var accessToken = tokens.RootElement.GetProperty("access_token").GetString()
            ?? throw new SecurityTokenException("Missing access token");
        string? subject = null;
        if (provider.MetadataAddress is not null)
        {
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
            subject = claims.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(subject))
                throw new SecurityTokenException("Missing subject");
        }
        var userInfoUrl = provider.UserInfoEndpoint;
        if (provider.Name == "Facebook")
        {
            var proof = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(provider.ClientSecret),
                Encoding.UTF8.GetBytes(accessToken))).ToLowerInvariant();
            userInfoUrl = QueryHelpers.AddQueryString(userInfoUrl, new Dictionary<string, string?>
            {
                ["fields"] = "id,name,email",
                ["appsecret_proof"] = proof
            });
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var data = user.RootElement;
        string? Field(string key) => data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        var userSubject = Field(provider.Name == "Facebook" ? "id" : "sub");
        if (string.IsNullOrEmpty(userSubject) || userSubject.Length > 256 || (subject is not null && userSubject != subject))
            throw new SecurityTokenException("Invalid userinfo subject");
        var email = Field("email");
        var verified = data.TryGetProperty("email_verified", out var emailVerified) && emailVerified.ValueKind == JsonValueKind.True;
        // Google is not authoritative for a third-party email without a hosted-domain claim.
        if (provider.Name == "Google" && email?.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) != true && Field("hd") is null)
            verified = false;
        return new(userSubject, email, Field("name"), verified);
    }
}
