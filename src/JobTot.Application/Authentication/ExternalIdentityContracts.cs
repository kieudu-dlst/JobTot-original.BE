namespace JobTot.Application.Authentication;

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


public static class ExternalAuthPolicy
{
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

}

public sealed class ExternalIdentityException(string message, Exception inner) : Exception(message, inner);
