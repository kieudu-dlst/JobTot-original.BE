using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JobTot.Application.Authentication;
using JobTot.Application.Companies;
using JobTot.Domain.Entities;
using JobTot.Infrastructure.Persistence;

public static class FeatureChecks
{
    public static async Task Run(IServiceProvider services, string origin, Action<bool, string> check)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IAccountPasswordHasher>();
        Account Account(string role)
        {
            var account = new Account { FullName = role + " architecture", Email = Guid.NewGuid() + "@checks.example", AccountType = role, AccountStatus = "Active" };
            account.PasswordHash = passwords.Hash(account, "ArchitectureCheck2026!");
            return account;
        }
        var candidate = Account("Candidate");
        candidate.CandidateProfile = new CandidateProfile { Account = candidate, AccountId = candidate.Id, ReadyStatus = "looking" };
        var employer = Account("Employer");
        employer.RecruiterProfiles.Add(new RecruiterProfile
        {
            Account = employer, Company = new Company("Architecture company", "") { VerificationStatus = "Verified" },
            IsCompanyAdmin = true, JoinedAt = DateTimeOffset.UtcNow,
        });
        db.AddRange(candidate, employer);
        await db.SaveChangesAsync();
        var candidateAccount = (await scope.ServiceProvider.GetRequiredService<CandidateAuthService>().GetActiveAsync(candidate.Id, default))!;
        var employerAccount = (await scope.ServiceProvider.GetRequiredService<EmployerAuthService>().GetActiveAsync(employer.Id, default))!;
        var candidateSession = await scope.ServiceProvider.GetRequiredService<CandidateTokens>().CreateAsync(candidateAccount, false, default);
        var employerSession = await scope.ServiceProvider.GetRequiredService<EmployerTokens>().CreateAsync(employerAccount, false, default);
        using var candidateClient = new HttpClient { BaseAddress = new Uri(origin) };
        using var employerClient = new HttpClient { BaseAddress = new Uri(origin) };
        candidateClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidateSession.AccessToken);
        employerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employerSession.AccessToken);
        async Task<JsonElement> Read(HttpResponseMessage response, string operation)
        {
            var text = await response.Content.ReadAsStringAsync();
            check(response.IsSuccessStatusCode, operation + (response.IsSuccessStatusCode ? "" : ": " + text));
            return JsonDocument.Parse(text).RootElement.Clone();
        }
        foreach (var route in new[] { "/api/employer/company", "/api/employer/campaigns", "/api/employer/candidates", "/api/employer/candidates/talents", "/api/employer/talent-discovery", "/api/employer/header" })
            await Read(await employerClient.GetAsync(route), "employer feature query: " + route);
        var company = await (await employerClient.GetAsync("/api/employer/company")).Content.ReadFromJsonAsync<EmployerCompanyResponse>();
        var originalRevision = company!.Document.Revision;
        company.Document.Name = "Updated architecture company";
        await Read(await employerClient.PutAsJsonAsync("/api/employer/company", company.Document), "company write commits through the application transaction boundary");
        company.Document.Revision = originalRevision;
        check((await employerClient.PutAsJsonAsync("/api/employer/company", company.Document)).StatusCode == HttpStatusCode.Conflict, "stale company revision cannot overwrite a newer edit");

        await Read(await candidateClient.GetAsync("/api/candidate/profile"), "candidate profile remains available after controller extraction");
        var started = await Read(await candidateClient.PostAsJsonAsync("/api/candidate/messages", new { accountId = employer.Id }), "candidate opens a conversation through the application service");
        var conversationId = started.GetProperty("id").GetGuid();
        var clientMessageId = Guid.NewGuid();
        await Read(await candidateClient.PostAsJsonAsync($"/api/candidate/messages/{conversationId}", new { text = "Hello", clientMessageId }), "chat send persists a message");
        await Read(await candidateClient.PostAsJsonAsync($"/api/candidate/messages/{conversationId}", new { text = "Hello", clientMessageId }), "repeated chat send is idempotent");
        var messages = await Read(await employerClient.GetAsync($"/api/employer/messages/{conversationId}"), "conversation peer can read messages");
        check(messages.GetProperty("items").GetArrayLength() == 1, "repeated client message ID produces one persisted message");
        check((await candidateClient.PostAsJsonAsync($"/api/candidate/messages/{conversationId}/typing", new { isTyping = true })).IsSuccessStatusCode, "typing updates use the transient-state port");
        var typing = await Read(await employerClient.GetAsync($"/api/employer/messages/{conversationId}/typing"), "peer typing state remains readable");
        check(typing.GetProperty("isTyping").GetBoolean(), "typing expiry survives the cache adapter");
        await Read(await candidateClient.GetAsync("/api/candidate/header"), "candidate header aggregation remains available");
        await WebSocketChecks.Run(candidateClient, employerClient, conversationId, check);
        using var invalidPdf = new MultipartFormDataContent();
        invalidPdf.Add(new ByteArrayContent("not a PDF"u8.ToArray()), "file", "fake.pdf");
        check((await candidateClient.PostAsync("/api/candidate/profile/cvs", invalidPdf)).StatusCode == HttpStatusCode.BadRequest, "application upload validation rejects a fake PDF");
    }
}
