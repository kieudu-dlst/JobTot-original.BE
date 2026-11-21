namespace JobTot.Application.Authentication;

public sealed record LoginOutcome<T>(T? Session = default, string? ChangeToken = null);
public sealed record EmployerRegistrationOutcome(EmployerRegistrationStatus Status, EmployerSessionDto? Session = null);

public sealed class CandidateAccess(CandidateAuthService accounts, CandidateTokens tokens, AccountEmailSecurity email)
{
    public async Task<CandidateSessionDto> RegisterAsync(CandidateRegisterRequest request, CancellationToken ct)
    {
        await email.RequireVerification(request.EmailOrPhone, "Candidate", request.EmailVerificationToken, ct);
        var account = await accounts.RegisterAsync(request, ct);
        return await tokens.CreateAsync(account, false, ct);
    }

    public async Task<LoginOutcome<CandidateSessionDto>> LoginAsync(CandidateLoginRequest request, CancellationToken ct)
    {
        var account = await accounts.LoginAsync(request, ct);
        if (account is not null)
            return new(await tokens.CreateAsync(account, request.RememberMe, ct));
        return new(ChangeToken: await email.TemporaryLogin(request.EmailOrPhone, "Candidate", request.Password, ct));
    }
}

public sealed class EmployerAccess(EmployerAuthService accounts, EmployerTokens tokens, AccountEmailSecurity email)
{
    public async Task<EmployerRegistrationOutcome> RegisterAsync(EmployerRegisterRequest request, CancellationToken ct)
    {
        await email.RequireVerification(request.Email, "Employer", request.EmailVerificationToken, ct);
        var result = await accounts.RegisterAsync(request, ct);
        return new(result.Status, result.Status == EmployerRegistrationStatus.Created
            ? await tokens.CreateAsync(result.Account!, false, ct) : null);
    }

    public async Task<LoginOutcome<EmployerSessionDto>> LoginAsync(EmployerLoginRequest request, CancellationToken ct)
    {
        var account = await accounts.LoginAsync(request, ct);
        if (account is not null)
            return new(await tokens.CreateAsync(account, request.RememberMe, ct));
        return new(ChangeToken: await email.TemporaryLogin(request.Email, "Employer", request.Password, ct));
    }
}
