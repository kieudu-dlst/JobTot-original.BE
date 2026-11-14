using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobTot.Api;
using JobTot.Api.Controllers;
using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

// This host uses an isolated database and captures email locally; it never sends real mail.
var output = Path.GetFullPath(args.FirstOrDefault() ?? ".artifacts/admin-user-checks");
Directory.CreateDirectory(output);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Development" });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddControllers().AddApplicationPart(typeof(AdminManagementController).Assembly);
// All request scopes must share the same database name.
var databaseName = $"admin-users-{Guid.NewGuid()}";
builder.Services.AddDbContext<RecruitmentDbContext>(o => o.UseInMemoryDatabase(databaseName));
builder.Services.AddSingleton<IAccountPasswordHasher, AccountPasswordHasher>();
builder.Services.AddScoped<ICandidateAuthRepository, CandidateAuthRepository>();
builder.Services.AddScoped<IEmployerAuthRepository, EmployerAuthRepository>();
builder.Services.AddScoped<CandidateAuthService>();
builder.Services.AddScoped<EmployerAuthService>();
builder.Services.AddCandidateAuthentication(builder.Configuration, true);
builder.Services.AddEmployerAuthentication();
builder.Services.AddAdminAuthentication();
builder.Services.AddScoped<AccountEmailSecurity>();
builder.Services.AddSingleton<CheckMail>();
builder.Services.AddSingleton<IAccountEmailSender>(s => s.GetRequiredService<CheckMail>());
builder.Services.AddSingleton<IRecoveryEmailSender>(s => s.GetRequiredService<CheckMail>());
builder.Services.AddScoped<PasswordRecovery>();
builder.Services.AddScoped<EmployerPasswordRecovery>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(output, "keys")));
await using var app = builder.Build();
app.UseExceptionHandler();
// Rate limiting is omitted in this test host so repeated auth checks remain deterministic.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
using var admin = new HttpClient { BaseAddress = client.BaseAddress };
var checks = new List<string>();
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    checks.Add(name);
    Console.WriteLine($"PASS {name}");
}
async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
{
    var body = await response.Content.ReadAsStringAsync();
    Check(response.StatusCode == expected, $"{response.RequestMessage!.RequestUri!.AbsolutePath}: HTTP {(int)expected}");
    return JsonSerializer.Deserialize<JsonElement>(body);
}
Guid adminId;
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
    var passwords = scope.ServiceProvider.GetRequiredService<IAccountPasswordHasher>();
    var account = new Account { FullName = "Admin kiểm thử", AccountType = "Admin", AccountStatus = "Active" };
    account.PasswordHash = passwords.Hash(account, "AdminPassword123!");
    var session = new CandidateRefreshSession
    {
        AccountId = account.Id, PasswordStamp = AdminAuthentication.Stamp(account.PasswordHash),
        TokenHash = Guid.NewGuid().ToString(), ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
    };
    db.AddRange(account, session);
    await db.SaveChangesAsync();
    adminId = account.Id;
    admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
        AdminAuthentication.Issue(scope.ServiceProvider.GetRequiredService<CandidateJwtSettings>(),
            account.Id, session.Id, account.FullName, session.ExpiresAt));
}
await Json(await admin.GetAsync("/api/admin/auth/me"));
Check((await admin.PutAsJsonAsync($"/api/admin/users/{adminId}/status", new { active = false })).StatusCode
    == HttpStatusCode.BadRequest, "Admin cannot lock the currently signed-in admin account");

foreach (var role in new[] { "candidate", "employer" })
{
    var email = $"{role}@admin-checks.example";
    const string oldPassword = "OriginalPassword123!";
    const string newPassword = "RecoveredPassword123!";
    Guid accountId;
    string hash;
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
        var account = new Account
        {
            FullName = "Người dùng kiểm thử", Email = email, AccountStatus = "Active",
            AccountType = role == "candidate" ? "Candidate" : "Employer", EmailVerifiedAt = DateTimeOffset.UtcNow,
        };
        account.PasswordHash = scope.ServiceProvider.GetRequiredService<IAccountPasswordHasher>().Hash(account, oldPassword);
        if (role == "candidate") account.CandidateProfile = new CandidateProfile();
        else account.RecruiterProfiles.Add(new RecruiterProfile
        {
            Company = new Company("Công ty kiểm thử", "") { VerificationStatus = "Verified" },
        });
        db.Add(account);
        await db.SaveChangesAsync();
        accountId = account.Id;
        hash = account.PasswordHash;
    }
    object Credentials(string password) => new { email, emailOrPhone = email, password };
    var session = await Json(await client.PostAsJsonAsync($"/api/{role}/auth/login", Credentials(oldPassword)));
    var removed = await admin.PutAsJsonAsync($"/api/admin/users/{accountId}/password", new { password = "AdminReplacement123!" });
    Check(removed.StatusCode == HttpStatusCode.NotFound, $"{role}: removed admin password endpoint returns 404 with a valid admin token");
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
        Check((await db.AccountRecords.SingleAsync(a => a.Id == accountId)).PasswordHash == hash,
            $"{role}: direct API attempt leaves the user's password unchanged");
    }
    Check((await admin.PutAsJsonAsync($"/api/admin/users/{accountId}/status", new { active = false })).StatusCode
        == HttpStatusCode.NoContent, $"{role}: admin can lock the account");
    Check((await client.PostAsJsonAsync($"/api/{role}/auth/login", Credentials(oldPassword))).StatusCode
        == HttpStatusCode.Unauthorized, $"{role}: locked account cannot log in");
    Check((await client.PostAsJsonAsync($"/api/{role}/auth/refresh", new { refreshToken = session.GetProperty("refreshToken").GetString() })).StatusCode
        == HttpStatusCode.Unauthorized, $"{role}: locking revokes existing sessions");
    Check((await admin.PutAsJsonAsync($"/api/admin/users/{accountId}/status", new { active = true })).StatusCode
        == HttpStatusCode.NoContent, $"{role}: admin can unlock the account");
    await Json(await client.PostAsJsonAsync($"/api/{role}/auth/login", Credentials(oldPassword)));
    await Json(await client.PostAsJsonAsync($"/api/{role}/auth/forgot-password", new { email }));
    var temporary = app.Services.GetRequiredService<CheckMail>().TemporaryPasswords[email];
    var change = await Json(await client.PostAsJsonAsync($"/api/{role}/auth/login", Credentials(temporary)), (HttpStatusCode)428);
    Check((await client.PostAsJsonAsync($"/api/{role}/auth/email/change-temporary-password", new
    {
        token = change.GetProperty("changeToken").GetString(), password = newPassword, confirmPassword = newPassword,
    })).StatusCode == HttpStatusCode.NoContent, $"{role}: user sets a new password through Forgot password");
    var recovered = await Json(await client.PostAsJsonAsync($"/api/{role}/auth/login", Credentials(newPassword)));
    Check(recovered.GetProperty("account").GetProperty("id").GetGuid() == accountId,
        $"{role}: login with the new password preserves the account");
    Check((await client.PostAsJsonAsync($"/api/{role}/auth/login", Credentials(oldPassword))).StatusCode
        == HttpStatusCode.Unauthorized, $"{role}: previous password no longer works");
}
await File.WriteAllTextAsync(Path.Combine(output, "backend-result.json"), JsonSerializer.Serialize(
    new { passed = true, checks, database = "isolated InMemory", email = "local fixture; no real mail sent" },
    new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"PASS {checks.Count} checks");
await app.StopAsync();

public sealed class CheckMail : IAccountEmailSender, IRecoveryEmailSender
{
    public Dictionary<string, string> TemporaryPasswords { get; } = [];
    public Task SendAsync(string email, string subject, string body, CancellationToken ct)
    {
        var match = Regex.Match(body, @"Mật khẩu tạm của bạn: ([A-F0-9]{24}a!)");
        if (match.Success) TemporaryPasswords[email] = match.Groups[1].Value;
        return Task.CompletedTask;
    }
    public Task SendAsync(string email, string link, CancellationToken ct) => Task.CompletedTask;
}
