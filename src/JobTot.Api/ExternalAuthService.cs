using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api;

public sealed record ExternalFlowData(string Role, string Provider, string Mode, string StateHash,
    string Verifier, string Nonce, string ReturnTo, string Stage = "authorizing",
    ExternalIdentity? Identity = null, string? Error = null);

public sealed class ExternalRegistrationRequest
{
    [Required, StringLength(200, MinimumLength = 2)] public string FullName { get; init; } = "";
    [Required, EmailAddress, StringLength(320)] public string Email { get; init; } = "";
    [StringLength(5000)]
    public string? EmailVerificationToken
    {
        get; init;
    }
    [Range(typeof(bool), "true", "true")]
    public bool ConfirmRole
    {
        get; init;
    }
    [Range(typeof(bool), "true", "true")]
    public bool AcceptTerms
    {
        get; init;
    }
    [StringLength(32)] public string WorkPhone { get; init; } = "";
    [StringLength(200)] public string CompanyName { get; init; } = "";
    [StringLength(14)] public string TaxCode { get; init; } = "";
    public Guid HeadOfficeProvinceId
    {
        get; init;
    }
    public bool RecruitmentCommitment
    {
        get; init;
    }
}

public sealed class ExternalLinkRequest
{
    [Required, StringLength(320)] public string Identity { get; init; } = "";
    [Required, StringLength(128)] public string Password { get; init; } = "";
    [Range(typeof(bool), "true", "true")]
    public bool ConfirmLink
    {
        get; init;
    }
    public bool RememberMe
    {
        get; init;
    }
}

public sealed class ExternalSessionLinkRequest
{
    [Range(typeof(bool), "true", "true")]
    public bool ConfirmLink
    {
        get; init;
    }
    public bool RememberMe
    {
        get; init;
    }
}
public sealed class ExternalPasswordChangeException(string token) : Exception
{
    public string Token { get; } = token;
}

public sealed class ExternalAuthService(RecruitmentDbContext db, IDataProtectionProvider protection,
    IExternalIdentityClient identities, IConfiguration configuration, IWebHostEnvironment environment,
    CandidateAuthService candidates, EmployerAuthService employers, CandidateTokens candidateTokens,
    EmployerTokens employerTokens, AccountEmailSecurity emailSecurity)
{
    private readonly IDataProtector protector = protection.CreateProtector("JobTot.ExternalAuth.v1");
    public static string NewSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string CookieName(Guid id) => $"jobtot.external.{id:N}";
    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public string Origin(string key)
    {
        var value = configuration[$"ExternalAuth:{key}"];
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.AbsolutePath != "/" ||
            uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0 ||
            (uri.Scheme != "https" && !(environment.IsDevelopment() && uri.Scheme == "http" && uri.IsLoopback)))
            throw new EmailSecurityException(503, "Đăng nhập mạng xã hội chưa được cấu hình địa chỉ ứng dụng hợp lệ.");
        return uri.GetLeftPart(UriPartial.Authority);
    }

    public string CallbackUrl(string role, string provider) => $"{Origin("ApiOrigin")}/api/{role}/auth/external/{provider.ToLowerInvariant()}/callback";
    public string FrontendUrl(string role, Guid? id = null, string? error = null) =>
        QueryHelpers.AddQueryString($"{Origin("WebOrigin")}/auth/external", new Dictionary<string, string?>
        {
            ["role"] = role,
            ["flow"] = id?.ToString(),
            ["error"] = error
        });

    public async Task<(ExternalAuthFlow Row, string Secret, string Url)> Start(string role, string provider,
        string mode, string? returnTo, CancellationToken ct)
    {
        if (mode is not ("login" or "register"))
            throw new EmailSecurityException(400, "Hình thức xác thực không hợp lệ.");
        var p = identities.Provider(provider, role); // Enforce role/provider before creating any state.
        Origin("WebOrigin");
        var callback = CallbackUrl(role, provider);
        var stateSecret = NewSecret();
        var browserSecret = NewSecret();
        var data = new ExternalFlowData(role, p.Name, mode, Hash(stateSecret), NewSecret(), NewSecret(), SafeReturn(role, returnTo));
        var row = new ExternalAuthFlow
        {
            BrowserSecretHash = Hash(browserSecret),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            ProtectedData = protector.Protect(JsonSerializer.Serialize(data)),
        };
        db.ExternalAuthFlows.Add(row);
        await db.SaveChangesAsync(ct);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = p.ClientId,
            ["redirect_uri"] = callback,
            ["response_type"] = "code",
            ["scope"] = p.Scope,
            ["state"] = $"{row.Id:N}.{stateSecret}",
            ["nonce"] = data.Nonce,
            ["code_challenge"] = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(data.Verifier))),
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account",
        };
        return (row, browserSecret, QueryHelpers.AddQueryString(p.AuthorizationEndpoint, query));
    }

    private ExternalFlowData Data(ExternalAuthFlow row)
    {
        try
        {
            return JsonSerializer.Deserialize<ExternalFlowData>(protector.Unprotect(row.ProtectedData))
                ?? throw new JsonException();
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            throw new EmailSecurityException(400, "Phiên xác thực không hợp lệ hoặc đã hết hạn. Vui lòng bắt đầu lại.");
        }
    }
    private void Update(ExternalAuthFlow row, ExternalFlowData data)
    {
        row.ProtectedData = protector.Protect(JsonSerializer.Serialize(data));
        row.Revision = Guid.NewGuid();
    }

    public async Task<(ExternalAuthFlow Row, ExternalFlowData Data)> Read(string role, Guid id,
        string? browserSecret, CancellationToken ct)
    {
        ExternalIdentityClient.Role(role);
        var row = await db.ExternalAuthFlows.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (row is null || row.ExpiresAt <= DateTimeOffset.UtcNow || browserSecret is null ||
            !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(row.BrowserSecretHash), Encoding.ASCII.GetBytes(Hash(browserSecret))))
            throw new EmailSecurityException(400, "Phiên xác thực không hợp lệ hoặc đã hết hạn. Vui lòng bắt đầu lại.");
        var data = Data(row);
        if (data.Role != role)
            throw new EmailSecurityException(403, "Vai trò không khớp với phiên xác thực.");
        ExternalIdentityClient.AllowedProvider(data.Provider, role);
        if (data.Stage == "consumed")
            throw new EmailSecurityException(409, "Phiên xác thực đã được sử dụng. Vui lòng bắt đầu lại.");
        return (row, data);
    }

    public async Task Callback(string role, string provider, string state, string? cookie,
        string? code, string? error, CancellationToken ct)
    {
        ExternalIdentityClient.AllowedProvider(provider, role);
        var pieces = state.Split('.');
        if (pieces.Length != 2 || !Guid.TryParseExact(pieces[0], "N", out var id))
            throw new EmailSecurityException(400, "Kết quả xác thực không hợp lệ.");
        var (row, data) = await Read(role, id, cookie, ct);
        if (!string.Equals(data.Provider, provider, StringComparison.OrdinalIgnoreCase) ||
            data.Stage != "authorizing" || Hash(pieces[1]) != data.StateHash)
            throw new EmailSecurityException(400, "Kết quả xác thực không khớp hoặc đã được sử dụng.");
        // Claim this callback before making external requests. Revision prevents concurrent replay.
        Update(row, data with
        {
            Stage = "exchanging"
        });
        await db.SaveChangesAsync(ct);
        if (!string.IsNullOrEmpty(error) || string.IsNullOrWhiteSpace(code))
        {
            Update(row, data with
            {
                Stage = "failed",
                Error = error == "access_denied"
                ? "Bạn đã hủy xác thực. Bạn có thể thử lại hoặc sử dụng email."
                : "Nhà cung cấp không hoàn tất xác thực. Vui lòng thử lại hoặc sử dụng email."
            });
        }
        else
        {
            try
            {
                var identity = await identities.Exchange(identities.Provider(provider, role), code,
                    CallbackUrl(role, provider), data.Verifier, data.Nonce, ct);
                Update(row, data with
                {
                    Stage = "ready",
                    Identity = identity,
                    Verifier = "",
                    Nonce = ""
                });
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException
                or Microsoft.IdentityModel.Tokens.SecurityTokenException or InvalidOperationException
                or KeyNotFoundException || exception is OperationCanceledException && !ct.IsCancellationRequested)
            {
                Update(row, data with
                {
                    Stage = "failed",
                    Verifier = "",
                    Nonce = "",
                    Error = "Không thể xác thực kết quả từ nhà cung cấp. Vui lòng thử lại hoặc sử dụng email."
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<ExternalLogin?> Link(ExternalFlowData data, CancellationToken ct) =>
        await db.ExternalLoginRecords.Include(x => x.Account).SingleOrDefaultAsync(
            x => x.Provider == data.Provider && x.ProviderSubject == data.Identity!.Subject, ct);

    private static void Ready(ExternalFlowData data)
    {
        if (data.Stage != "ready" || data.Identity is null)
            throw new EmailSecurityException(400, data.Error ?? "Chưa hoàn tất xác thực với nhà cung cấp.");
    }

    public async Task<object> Describe(string role, Guid id, string? cookie, CancellationToken ct)
    {
        var (_, data) = await Read(role, id, cookie, ct);
        var link = data.Stage == "ready" ? await Link(data, ct) : null;
        var duplicate = data.Identity?.Email is { } email && await db.AccountRecords.AnyAsync(x => x.Email == Normalize(email), ct);
        var wrongRole = link is not null && link.Account.AccountType != ExternalIdentityClient.Role(role);
        return new
        {
            flowId = id,
            role,
            provider = data.Provider,
            mode = data.Mode,
            stage = data.Stage,
            fullName = data.Identity?.Name,
            email = data.Identity?.Email,
            emailVerified = data.Identity?.EmailVerified == true,
            duplicateEmail = duplicate,
            linked = link is not null && !wrongRole,
            wrongRole,
            error = data.Error,
            returnTo = data.ReturnTo,
        };
    }

    // All completion writes, link ownership, email-code consumption and refresh-session issuance
    // share one transaction. A concurrent completion rolls back rather than orphaning an account.
    private async Task<object> Complete(string role, Guid id, string? cookie,
        Func<ExternalFlowData, CancellationToken, Task<object>> action, CancellationToken ct)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = db.Database.IsRelational()
                    ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
                var (row, data) = await Read(role, id, cookie, ct);
                Ready(data);
                var result = await action(data, ct);
                Update(row, data with
                {
                    Stage = "consumed"
                });
                await db.SaveChangesAsync(ct);
                if (transaction is not null)
                    await transaction.CommitAsync(ct);
                return result;
            });
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            throw new EmailSecurityException(409, "Thông tin tài khoản hoặc liên kết đã được sử dụng. Vui lòng đăng nhập hoặc bắt đầu xác thực lại.");
        }
    }

    private async Task<object> Session(string role, Guid accountId, bool remember, CancellationToken ct)
    {
        if (role == "candidate")
        {
            var account = await candidates.GetActiveAsync(accountId, ct)
                ?? throw new EmailSecurityException(403, "Tài khoản ứng viên không khả dụng.");
            return await candidateTokens.CreateAsync(account, remember, ct);
        }
        var employer = await employers.GetActiveAsync(accountId, ct)
            ?? throw new EmailSecurityException(403, "Tài khoản nhà tuyển dụng không khả dụng.");
        return await employerTokens.CreateAsync(employer, remember, ct);
    }

    public Task<object> Login(string role, Guid id, string? cookie, bool remember, CancellationToken ct) =>
        Complete(role, id, cookie, async (data, token) =>
        {
            var link = await Link(data, token);
            if (link is null)
                throw new EmailSecurityException(409, "Tài khoản chưa được liên kết. Vui lòng đăng ký và xác nhận vai trò trước khi tạo tài khoản.");
            if (link.Status != EntityStatus.Active || link.Account.AccountType != ExternalIdentityClient.Role(role))
                throw new EmailSecurityException(403, "Tài khoản đã liên kết không thuộc vai trò này hoặc không khả dụng.");
            return await Session(role, link.AccountId, remember, token);
        }, ct);

    public Task<object> Register(string role, Guid id, string? cookie, ExternalRegistrationRequest request, CancellationToken ct) =>
        Complete(role, id, cookie, async (data, token) =>
        {
            if (data.Mode != "register")
                throw new EmailSecurityException(409, "Vui lòng chuyển sang đăng ký và xác nhận vai trò trước khi tạo tài khoản.");
            Validate(request);
            if (await Link(data, token) is not null)
                throw new EmailSecurityException(409, "Tài khoản nhà cung cấp đã được liên kết. Vui lòng đăng nhập.");
            var email = Normalize(request.Email);
            var providerEmail = Normalize(data.Identity!.Email ?? "");
            if (await db.AccountRecords.AnyAsync(x => x.Email == email || (providerEmail != "" && x.Email == providerEmail), token))
                throw new EmailSecurityException(409, "Email đã có tài khoản Job Tốt. Hãy đăng nhập tài khoản đó để xác nhận liên kết.");
            if (data.Identity!.EmailVerified != true || Normalize(data.Identity.Email ?? "") != email)
                await emailSecurity.RequireVerification(email, ExternalIdentityClient.Role(role), request.EmailVerificationToken, token);
            var link = new ExternalLogin { Provider = data.Provider, ProviderSubject = data.Identity.Subject };
            // An unguessable password preserves existing password stamps/recovery without adding a usable password.
            var password = NewSecret();
            var employerRequest = role == "employer" ? new EmployerRegisterRequest
            {
                FullName = request.FullName,
                Email = email,
                WorkPhone = request.WorkPhone,
                CompanyName = request.CompanyName,
                TaxCode = request.TaxCode,
                HeadOfficeProvinceId = request.HeadOfficeProvinceId,
                AcceptTerms = request.AcceptTerms,
                RecruitmentCommitment = request.RecruitmentCommitment,
                Password = password,
                ConfirmPassword = password,
            } : null;
            if (employerRequest is not null)
                Validate(employerRequest);
            Guid accountId;
            if (role == "candidate")
                accountId = (await candidates.RegisterAsync(new CandidateRegisterRequest
                {
                    FullName = request.FullName,
                    EmailOrPhone = email,
                    Password = password,
                    ConfirmPassword = password
                }, token, link)).Id;
            else
            {
                var result = await employers.RegisterAsync(employerRequest!, token, link);
                if (result.Status == EmployerRegistrationStatus.DuplicateAccount)
                    throw new EmailSecurityException(409, "Email hoặc số điện thoại đã được đăng ký. Hãy đăng nhập tài khoản Job Tốt để liên kết.");
                if (result.Status == EmployerRegistrationStatus.CompanyConflict)
                    throw new EmployerCompanyConflictException();
                accountId = result.Account!.Id;
            }
            return await Session(role, accountId, false, token);
        }, ct);

    public async Task<object> ConfirmRegistration(string role, Guid id, string? cookie, CancellationToken ct)
    {
        var (row, data) = await Read(role, id, cookie, ct);
        Ready(data);
        Update(row, data with
        {
            Mode = "register"
        });
        await db.SaveChangesAsync(ct);
        return await Describe(role, id, cookie, ct);
    }

    public async Task<object> ConfirmLink(string role, Guid id, string? cookie, ExternalLinkRequest request, CancellationToken ct)
    {
        var (_, pending) = await Read(role, id, cookie, ct);
        Ready(pending);
        Validate(request);
        // Authentication attempts must persist even when linking is rejected or requires
        // a temporary-password change; keep them outside the completion transaction.
        Guid accountId;
        if (role == "candidate")
            accountId = (await candidates.LoginAsync(new CandidateLoginRequest
            {
                EmailOrPhone = request.Identity,
                Password = request.Password
            }, ct))?.Id
                ?? await PasswordFailure(role, request, ct);
        else
            accountId = (await employers.LoginAsync(new EmployerLoginRequest
            {
                Email = request.Identity,
                Password = request.Password
            }, ct))?.Id
                ?? await PasswordFailure(role, request, ct);
        return await Complete(role, id, cookie,
            (data, token) => LinkAccount(role, data, accountId, request.RememberMe, token), ct);
    }

    private async Task<Guid> PasswordFailure(string role, ExternalLinkRequest request, CancellationToken ct)
    {
        var change = await emailSecurity.TemporaryLogin(request.Identity, ExternalIdentityClient.Role(role), request.Password, ct);
        if (change is not null)
            throw new ExternalPasswordChangeException(change);
        throw new EmailSecurityException(401, "Thông tin đăng nhập Job Tốt không đúng hoặc tài khoản không khả dụng.");
    }

    public Task<object> ConfirmSessionLink(string role, Guid id, string? cookie, Guid accountId,
        ExternalSessionLinkRequest request, CancellationToken ct) => Complete(role, id, cookie,
        async (data, token) =>
        {
            Validate(request);
            return await LinkAccount(role, data, accountId, request.RememberMe, token);
        }, ct);

    private async Task<object> LinkAccount(string role, ExternalFlowData data, Guid accountId,
        bool remember, CancellationToken token)
    {
        var account = await db.AccountRecords.SingleOrDefaultAsync(x => x.Id == accountId, token);
        if (account?.AccountType != ExternalIdentityClient.Role(role))
            throw new EmailSecurityException(403, "Tài khoản Job Tốt không thuộc vai trò đã chọn.");
        var existing = await Link(data, token);
        var providerEmailAccount = data.Identity!.Email is { } email
            ? await db.AccountRecords.SingleOrDefaultAsync(x => x.Email == Normalize(email), token) : null;
        if (providerEmailAccount is not null && providerEmailAccount.Id != accountId)
            throw new EmailSecurityException(409, "Vui lòng đăng nhập đúng tài khoản Job Tốt có email trùng với nhà cung cấp để xác nhận liên kết.");
        if (existing is not null && (existing.AccountId != accountId || existing.Status != EntityStatus.Active))
            throw new EmailSecurityException(409, "Tài khoản nhà cung cấp đã liên kết với tài khoản Job Tốt khác hoặc đã bị khóa.");
        if (existing is null)
        {
            db.ExternalLoginRecords.Add(new ExternalLogin
            {
                AccountId = accountId,
                Provider = data.Provider,
                ProviderSubject = data.Identity!.Subject
            });
            await db.SaveChangesAsync(token);
        }
        return await Session(role, accountId, remember, token);
    }

    private static void Validate(object request)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new EmailSecurityException(400, "Vui lòng điền đầy đủ thông tin và xác nhận vai trò, điều khoản hoặc liên kết tài khoản.");
    }

    private static string SafeReturn(string role, string? path)
    {
        if (path is not null && System.Text.RegularExpressions.Regex.IsMatch(path,
            @"^/auth/external\?role=(candidate|employer)&flow=[0-9a-fA-F-]{36}$"))
            return path;
        if (role == "employer")
            return "/employer/account";
        // Only application-relative destinations; reject protocol-relative URLs and backslashes.
        return path is { Length: > 0 and < 1000 } && path.StartsWith('/') && !path.StartsWith("//")
            && !path.Contains('\\') && !path.Any(char.IsControl) &&
            (path.StartsWith("/jobs") || path.StartsWith("/my-jobs") || path.StartsWith("/messages")) ? path : "/jobs";
    }
}
