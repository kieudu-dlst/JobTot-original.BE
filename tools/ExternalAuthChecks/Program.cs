using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobTot.Api;
using JobTot.Api.Controllers;
using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using JobTot.Infrastructure;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

const string origin = "http://127.0.0.1:5199";
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
// Run from repository root, or pass --root <workspace> when using a custom output directory.
var rootArg = Array.IndexOf(args, "--root");
if (rootArg >= 0) root = Path.GetFullPath(args[rootArg + 1]);
var output = Path.Combine(root, ".artifacts/external-auth-checks");
Directory.CreateDirectory(output);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development", Args = [] });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls(origin);
builder.Configuration["ExternalAuth:ApiOrigin"] = origin;
builder.Configuration["ExternalAuth:WebOrigin"] = origin;
builder.Services.AddControllers().AddApplicationPart(typeof(ExternalAuthController).Assembly);
builder.Services.AddDbContext<RecruitmentDbContext>(options => options.UseInMemoryDatabase("external-auth-checks"));
builder.Services.AddSingleton<IAccountPasswordHasher, AccountPasswordHasher>();
builder.Services.AddScoped<ICandidateAuthRepository, CandidateAuthRepository>();
builder.Services.AddScoped<IEmployerAuthRepository, EmployerAuthRepository>();
builder.Services.AddScoped<CandidateAuthService>();
builder.Services.AddScoped<EmployerAuthService>();
builder.Services.AddCandidateAuthentication(builder.Configuration, true);
builder.Services.AddEmployerAuthentication();
builder.Services.AddScoped<ExternalAuthService>();
builder.Services.AddSingleton<IExternalIdentityClient, FakeIdentityClient>();
builder.Services.AddScoped<AccountEmailSecurity>();
builder.Services.AddSingleton<TestMail>();
builder.Services.AddSingleton<IAccountEmailSender>(services => services.GetRequiredService<TestMail>());
builder.Services.AddSingleton<IRecoveryEmailSender>(services => services.GetRequiredService<TestMail>());
builder.Services.AddScoped<PasswordRecovery>();
builder.Services.AddScoped<EmployerPasswordRecovery>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(output, "keys")));
await using var app = builder.Build();
app.UseExceptionHandler();
// Keep the existing auth rate limiter out of this fixture host: the scenario suite
// intentionally exceeds ten requests/minute. Production still enables its named policy.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/checks/authorize/{provider}", (string provider, HttpRequest request) =>
{
    var callback = request.Query["redirect_uri"].ToString();
    var role = callback.Contains("/employer/") ? "employer" : "candidate";
    var state = request.Query["state"].ToString();
    var approve = QueryHelpers.AddQueryString(callback, new Dictionary<string, string?> { ["state"] = state, ["code"] = $"{provider}-{role}" });
    var cancel = QueryHelpers.AddQueryString(callback, new Dictionary<string, string?> { ["state"] = state, ["error"] = "access_denied" });
    return Results.Content($"<html><body><h1>Nhà cung cấp giả lập {WebUtility.HtmlEncode(provider)}</h1><a id='approve' href='{WebUtility.HtmlEncode(approve)}'>Đồng ý</a><a id='cancel' href='{WebUtility.HtmlEncode(cancel)}'>Hủy</a></body></html>", "text/html; charset=utf-8");
});
app.MapGet("/checks/mail", (string email, TestMail mail) => Results.Ok(new { code = mail.Codes.GetValueOrDefault(email) }));
app.MapGet("/checks/temporary-password", (string email, TestMail mail) => Results.Ok(new { password = mail.TemporaryPasswords.GetValueOrDefault(email) }));
var dist = Path.Combine(root, "FE/dist");
if (Directory.Exists(dist))
{
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(dist) });
    app.MapFallback(() => Results.File(Path.Combine(dist, "index.html"), "text/html"));
}
await app.StartAsync();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
    db.ProvinceRecords.Add(new Province { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), ProvinceName = "Hồ Chí Minh", ProvinceCode = "79" });
    await db.SaveChangesAsync();
}
if (args.Contains("--serve"))
{
    Console.WriteLine($"READY {origin} (isolated InMemory database and provider fixtures)");
    await app.WaitForShutdownAsync();
    return;
}
var checks = new List<string>();
void Check(bool value, string name)
{
    if (!value)
        throw new InvalidOperationException(name);
    checks.Add(name);
    Console.WriteLine($"PASS {name}");
}
using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
using var client = new HttpClient(handler) { BaseAddress = new Uri(origin) };
client.DefaultRequestHeaders.Add("X-JobTot-External", "1");
async Task<JsonElement> Json(HttpResponseMessage response, int status = 200)
{
    var text = await response.Content.ReadAsStringAsync();
    Check((int)response.StatusCode == status, $"HTTP {status}: {response.RequestMessage?.RequestUri?.AbsolutePath} ({(int)response.StatusCode}) {((int)response.StatusCode == status ? "" : text)}");
    return JsonDocument.Parse(text).RootElement.Clone();
}
async Task<(Guid Id, string State, string Cookie)> Start(string role, string provider, string mode, string code, string? error = null)
{
    var start = await Json(await client.PostAsJsonAsync($"/api/{role}/auth/external/{provider}/start", new
    {
        mode
    }));
    var uri = new Uri(start.GetProperty("authorizationUrl").GetString()!);
    var query = QueryHelpers.ParseQuery(uri.Query);
    var state = query["state"].ToString();
    var id = Guid.ParseExact(state.Split('.')[0], "N");
    var callback = QueryHelpers.AddQueryString(query["redirect_uri"].ToString(), new Dictionary<string, string?> { ["state"] = state, ["code"] = code, ["error"] = error });
    var response = await client.GetAsync(callback);
    Check(response.StatusCode == HttpStatusCode.Redirect, $"{provider}/{role}/{mode}: callback returns to frontend");
    return (id, state, handler.CookieContainer.GetCookies(new Uri(origin + "/api"))[ExternalAuthService.CookieName(id)]!.Value);
}
object Registration(string email, string? emailVerificationToken = null, bool employer = false) => new
{
    fullName = "Người dùng kiểm thử",
    email,
    emailVerificationToken,
    confirmRole = true,
    acceptTerms = true,
    workPhone = "0987654321",
    companyName = "Công ty kiểm thử",
    taxCode = "0123456789",
    headOfficeProvinceId = "00000000-0000-0000-0000-000000000001",
    recruitmentCommitment = employer,
};
foreach (var provider in new[] { "google", "facebook", "linkedin" })
{
    var role = "candidate";
    var email = $"{provider}.backend@checks.example";
    var flow = await Start(role, provider, "register", $"{provider}-backend");
    string? verification = null;
    if (provider == "facebook")
    {
        await Json(await client.PostAsJsonAsync($"/api/{role}/auth/external/flows/{flow.Id}/register", Registration(email)), 400);
        using var scope = app.Services.CreateScope();
        var security = scope.ServiceProvider.GetRequiredService<AccountEmailSecurity>();
        var id = await security.RequestVerification(email, "Candidate", default);
        verification = (await security.Verify(id, email, "Candidate", app.Services.GetRequiredService<TestMail>().Codes[email], default)).Token;
    }
    var session = await Json(await client.PostAsJsonAsync($"/api/{role}/auth/external/flows/{flow.Id}/register", Registration(email, verification)));
    Check(session.GetProperty("account").GetProperty("accountType").GetString() == "Candidate", $"{provider}: candidate created only after completion");
    await Json(await client.PostAsJsonAsync($"/api/{role}/auth/external/flows/{flow.Id}/register", Registration(email, verification)), 409);
    var login = await Start(role, provider, "login", $"{provider}-backend");
    var signedIn = await Json(await client.PostAsJsonAsync($"/api/{role}/auth/external/flows/{login.Id}/session", new
    {
        rememberMe = false
    }));
    Check(session.GetProperty("account").GetProperty("id").GetString() == signedIn.GetProperty("account").GetProperty("id").GetString(), $"{provider}: linked login reuses account");
    var refresh = signedIn.GetProperty("refreshToken").GetString();
    var renewed = await Json(await client.PostAsJsonAsync("/api/candidate/auth/refresh", new
    {
        refreshToken = refresh
    }));
    await client.PostAsJsonAsync("/api/candidate/auth/logout", new
    {
        refreshToken = renewed.GetProperty("refreshToken").GetString()
    });
    var denied = await client.PostAsJsonAsync("/api/candidate/auth/refresh", new
    {
        refreshToken = renewed.GetProperty("refreshToken").GetString()
    });
    Check(denied.StatusCode == HttpStatusCode.Unauthorized, $"{provider}: existing refresh/logout lifecycle works");
}
var employerFlow = await Start("employer", "google", "register", "google-employerbackend");
await Json(await client.PostAsJsonAsync($"/api/employer/auth/external/flows/{employerFlow.Id}/register", new { fullName = "HR", email = "google.employerbackend@checks.example", confirmRole = true, acceptTerms = true }), 400);
var employerSession = await Json(await client.PostAsJsonAsync($"/api/employer/auth/external/flows/{employerFlow.Id}/register", Registration("google.employerbackend@checks.example", employer: true)));
Check(employerSession.GetProperty("account").GetProperty("verificationStatus").GetString() == "Pending", "Google employer retains company verification workflow");
var employerLogin = await Start("employer", "google", "login", "google-employerbackend");
await Json(await client.PostAsJsonAsync($"/api/employer/auth/external/flows/{employerLogin.Id}/session", new { rememberMe = true }));
foreach (var provider in new[] { "facebook", "linkedin" })
{
    await Json(await client.PostAsJsonAsync($"/api/employer/auth/external/{provider}/start", new
    {
        mode = "register"
    }), 403);
    var forbidden = await client.GetAsync($"/api/employer/auth/external/{provider}/callback?state=bad&code=bad");
    Check(forbidden.StatusCode == HttpStatusCode.Forbidden, $"employer direct {provider} callback denied");
}
var missing = await Start("candidate", "google", "login", "missing");
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{missing.Id}/session", new { }), 409);
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{missing.Id}/register", Registration("added@checks.example")), 409);
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{missing.Id}/registration", new { }));
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{missing.Id}/register", Registration("added@checks.example")), 400);
using (var scope = app.Services.CreateScope())
{
    var security = scope.ServiceProvider.GetRequiredService<AccountEmailSecurity>();
    var challenge = await security.RequestVerification("added@checks.example", "Candidate", default);
    var proof = await security.Verify(challenge, "added@checks.example", "Candidate", app.Services.GetRequiredService<TestMail>().Codes["added@checks.example"], default);
    await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{missing.Id}/register", Registration("added@checks.example", proof.Token)));
}
var cancel = await Start("candidate", "google", "login", "unused", "access_denied");
var canceled = await Json(await client.GetAsync($"/api/candidate/auth/external/flows/{cancel.Id}"));
Check(canceled.GetProperty("error").GetString()!.Contains("hủy"), "cancellation has Vietnamese message");
var failed = await Start("candidate", "google", "login", "provider-error");
var failure = await Json(await client.GetAsync($"/api/candidate/auth/external/flows/{failed.Id}"));
Check(failure.GetProperty("stage").GetString() == "failed", "provider validation failure creates no session");
var switched = await Start("candidate", "google", "login", "google-backend");
await Json(await client.GetAsync($"/api/employer/auth/external/flows/{switched.Id}"), 403);
using (var attacker = new HttpClient { BaseAddress = new Uri(origin) })
{
    var result = await attacker.GetAsync($"/api/candidate/auth/external/flows/{switched.Id}");
    Check(result.StatusCode == HttpStatusCode.BadRequest, "flow cannot be read without browser cookie");
    result = await attacker.PostAsJsonAsync("/api/candidate/auth/external/google/start", new
    {
        mode = "login"
    });
    Check(result.StatusCode == HttpStatusCode.BadRequest, "cross-site start without intent header denied");
}
// An existing email must be confirmed with that account's credentials; no automatic email linking.
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<CandidateAuthService>();
    await service.RegisterAsync(new CandidateRegisterRequest { FullName = "Tài khoản cũ", EmailOrPhone = "duplicate@checks.example", Password = "Password123!", ConfirmPassword = "Password123!" }, default);
}
var duplicate = await Start("candidate", "google", "register", "duplicate");
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{duplicate.Id}/register", Registration("another@checks.example")), 409);
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{duplicate.Id}/link", new { identity = "duplicate@checks.example", password = "wrong", confirmLink = true }), 401);
var linkedSession = await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{duplicate.Id}/link", new { identity = "duplicate@checks.example", password = "Password123!", confirmLink = true }));
var sessionLink = await Start("candidate", "facebook", "login", "duplicate");
client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", linkedSession.GetProperty("accessToken").GetString());
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{sessionLink.Id}/link-session", new { confirmLink = false }), 400);
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{sessionLink.Id}/link-session", new { confirmLink = true }));
client.DefaultRequestHeaders.Authorization = null;
await Json(await client.PostAsJsonAsync("/api/candidate/auth/forgot-password", new { email = "duplicate@checks.example" }));
var temporaryFlow = await Start("candidate", "linkedin", "login", "duplicate");
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{temporaryFlow.Id}/link", new { identity = "duplicate@checks.example", password = "wrong-temporary-password", confirmLink = true }), 401);
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
    var challenge = await db.AccountEmailChallenges.SingleAsync(x => x.Email == "duplicate@checks.example" && x.Purpose == "temporary-password");
    Check(challenge.Attempts == 1, "failed temporary-password attempts persist independently of linking");
}
var temporary = app.Services.GetRequiredService<TestMail>().TemporaryPasswords["duplicate@checks.example"];
var changePassword = await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{temporaryFlow.Id}/link", new { identity = "duplicate@checks.example", password = temporary, confirmLink = true }), 428);
var changeResponse = await client.PostAsJsonAsync("/api/candidate/auth/email/change-temporary-password", new { token = changePassword.GetProperty("changeToken").GetString(), password = "ChangedPassword123!", confirmPassword = "ChangedPassword123!" });
Check(changeResponse.StatusCode == HttpStatusCode.NoContent, "existing temporary-password change completes before linking");
await Json(await client.PostAsJsonAsync($"/api/candidate/auth/external/flows/{temporaryFlow.Id}/link", new { identity = "duplicate@checks.example", password = "ChangedPassword123!", confirmLink = true }));
var expired = await Start("candidate", "google", "login", "expired");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
    var row = await db.ExternalAuthFlows.SingleAsync(x => x.Id == expired.Id);
    row.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
    await db.SaveChangesAsync();
}
await Json(await client.GetAsync($"/api/candidate/auth/external/flows/{expired.Id}"), 400);
var replay = await client.GetAsync($"/api/candidate/auth/external/google/callback?state={switched.State}&code=google-backend");
Check(replay.Headers.Location?.Query.Contains("error=invalid_state") == true, "callback replay is rejected");
var authorizing = await Json(await client.PostAsJsonAsync("/api/candidate/auth/external/google/start", new { mode = "login", returnTo = "//evil.example/path" }));
var authQuery = QueryHelpers.ParseQuery(new Uri(authorizing.GetProperty("authorizationUrl").GetString()!).Query);
var tampered = authQuery["state"].ToString() + "tamper";
var tamper = await client.GetAsync($"/api/candidate/auth/external/google/callback?state={tampered}&code=google-backend");
Check(tamper.Headers.Location?.Query.Contains("error=invalid_state") == true, "tampered OAuth state is rejected");
var intact = await client.GetAsync(QueryHelpers.AddQueryString(authQuery["redirect_uri"].ToString(), new Dictionary<string, string?> { ["state"] = authQuery["state"], ["code"] = "google-backend" }));
var intactId = Guid.ParseExact(authQuery["state"].ToString().Split('.')[0], "N");
var safe = await Json(await client.GetAsync($"/api/candidate/auth/external/flows/{intactId}"));
Check(safe.GetProperty("returnTo").GetString() == "/jobs", "unsafe external return URL is discarded");
var wrongRole = await Start("employer", "google", "login", "google-backend");
var wrongRoleData = await Json(await client.GetAsync($"/api/employer/auth/external/flows/{wrongRole.Id}"));
Check(wrongRoleData.GetProperty("wrongRole").GetBoolean(), "provider identity linked to other role is rejected");
await Json(await client.PostAsJsonAsync($"/api/employer/auth/external/flows/{wrongRole.Id}/session", new { }), 403);
await ProtocolChecks.Run(Check);
await File.WriteAllTextAsync(Path.Combine(output, "backend-result.json"), JsonSerializer.Serialize(new { passed = true, checks, database = "isolated InMemory; no real account or database modified", providers = "fixtures, not live accounts" }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS {checks.Count} backend/protocol checks");
await app.StopAsync();

public sealed class FakeIdentityClient : IExternalIdentityClient
{
    public ExternalProvider Provider(string provider, string role)
    {
        var name = ExternalIdentityClient.AllowedProvider(provider, role);
        return new(name, "checks", "checks", $"http://127.0.0.1:5199/checks/authorize/{provider}", "", "", null, "checks");
    }
    public Task<ExternalIdentity> Exchange(ExternalProvider provider, string code, string callback, string verifier, string nonce, CancellationToken ct)
    {
        if (code == "provider-error")
            throw new Microsoft.IdentityModel.Tokens.SecurityTokenException("provider failure");
        var email = code == "missing" ? null : code == "duplicate" ? "duplicate@checks.example" : code.Replace('-', '.') + "@checks.example";
        return Task.FromResult(new ExternalIdentity(code, email, code == "missing" ? null : "Người dùng thử nghiệm", provider.Name != "Facebook"));
    }
}
public sealed class TestMail : IAccountEmailSender, IRecoveryEmailSender
{
    public System.Collections.Concurrent.ConcurrentDictionary<string, string> Codes { get; } = new();
    public System.Collections.Concurrent.ConcurrentDictionary<string, string> TemporaryPasswords { get; } = new();
    public Task SendAsync(string email, string subject, string body, CancellationToken ct)
    {
        Codes[email] = Regex.Match(body, @"\b\d{6}\b").Value;
        var temporary = Regex.Match(body, @"Mật khẩu tạm của bạn: ([A-F0-9]{24}a!)");
        if (temporary.Success)
            TemporaryPasswords[email] = temporary.Groups[1].Value;
        return Task.CompletedTask;
    }
    public Task SendAsync(string email, string link, CancellationToken ct) => Task.CompletedTask;
}
