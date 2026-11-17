using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using JobTot.Api;
using Microsoft.IdentityModel.Tokens;

public static class ProtocolChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ExternalAuth:Google:ClientId"] = "google-client",
            ["ExternalAuth:Google:ClientSecret"] = "secret",
        }).Build();
        using var handler = new ProtocolHandler();
        using var http = new HttpClient(handler);
        var client = new ExternalIdentityClient(config, new Factory(http));
        const string name = "google";
        foreach (var role in new[] { "candidate", "employer" })
        {
            handler.Fault = "";
            var identity = await client.Exchange(client.Provider(name, role), "code", "https://jobtot.example/callback", "verifier", "nonce", default);
            check(identity.Subject == "stable-subject", $"{role} {name}: real protocol client accepts valid backchannel response");
            foreach (var fault in new[] { "signature", "audience", "issuer", "expired", "nonce", "subject", "authorized-party", "multiple-audiences" })
            {
                handler.Fault = fault;
                try
                {
                    await client.Exchange(client.Provider(name, role), "code", "https://jobtot.example/callback", "verifier", "nonce", default);
                    check(false, $"{role} {name}: rejects invalid {fault}");
                }
                catch (SecurityTokenException) { check(true, $"{role} {name}: rejects invalid {fault}"); }
            }
        }
        try
        {
            new ExternalIdentityClient(new ConfigurationBuilder().Build(), new Factory(http)).Provider("google", "candidate");
            check(false, "unconfigured provider rejected");
        }
        catch (EmailSecurityException ex) { check(ex.Status == 503 && ex.Message.Contains("chưa được cấu hình"), "unconfigured provider has Vietnamese 503 response"); }
        foreach (var role in new[] { "candidate", "employer" })
        foreach (var provider in new[] { "facebook", "linkedin", "Facebook", "LINKEDIN" })
        {
            try
            {
                client.Provider(provider, role);
                check(false, "provider policy");
            }
            catch (EmailSecurityException ex) { check(ex.Status == 400, $"{role} {provider}: rejected before configuration or network access"); }
        }
    }
    private sealed class Factory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
    private sealed class ProtocolHandler : HttpMessageHandler
    {
        private readonly RSA key = RSA.Create(2048);
        private readonly RSA invalid = RSA.Create(2048);
        public string Fault { get; set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            const string issuer = "https://accounts.google.com";
            object result;
            var url = request.RequestUri!.AbsoluteUri;
            if (url.Contains(".well-known"))
                result = new
                {
                    issuer,
                    jwks_uri = $"{issuer}/jwks"
                };
            else if (url.EndsWith("/jwks"))
            {
                var parameters = key.ExportParameters(false);
                result = new
                {
                    keys = new[] { new { kty = "RSA", use = "sig", alg = "RS256", kid = "test", n = Base64UrlEncoder.Encode(parameters.Modulus), e = Base64UrlEncoder.Encode(parameters.Exponent) } }
                };
            }
            else if (request.Method == HttpMethod.Post)
            {
                var body = await request.Content!.ReadAsStringAsync(ct);
                if (!body.Contains("client_secret=secret") || !body.Contains("code_verifier=verifier"))
                    throw new InvalidOperationException("Token request missing client authentication or PKCE");
                var credentials = new SigningCredentials(new RsaSecurityKey(Fault == "signature" ? invalid : key) { KeyId = "test" }, SecurityAlgorithms.RsaSha256);
                var token = new JwtSecurityToken(Fault == "issuer" ? "https://evil.example" : issuer,
                    Fault == "audience" ? "wrong-client" : "google-client",
                    [new Claim("sub", "stable-subject"), new Claim("nonce", Fault == "nonce" ? "wrong" : "nonce")],
                    DateTime.UtcNow.AddMinutes(-10), Fault == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5), credentials);
                if (Fault == "authorized-party")
                    token.Payload["azp"] = "wrong-client";
                if (Fault == "multiple-audiences")
                    token.Payload["aud"] = new[] { "google-client", "another-client" };
                result = new
                {
                    access_token = "test-access",
                    id_token = new JwtSecurityTokenHandler().WriteToken(token),
                    token_type = "Bearer"
                };
            }
            else
            {
                if (request.Headers.Authorization?.ToString() != "Bearer test-access")
                    throw new InvalidOperationException("userinfo requires provider token");
                result = new
                {
                    sub = Fault == "subject" ? "other-subject" : "stable-subject",
                    name = "Người dùng",
                    email = "person@gmail.com",
                    email_verified = true
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(result), System.Text.Encoding.UTF8, "application/json") };
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                key.Dispose();
                invalid.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
