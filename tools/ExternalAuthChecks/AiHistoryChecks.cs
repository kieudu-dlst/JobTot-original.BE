using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JobTot.Api;
using JobTot.Api.Controllers;
using JobTot.Application.Ai;
using JobTot.Application.Authentication;
using JobTot.Application.Execution;
using JobTot.Domain.Entities;
using JobTot.Infrastructure;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

public static class AiHistoryChecks
{
    public static async Task SqlRead()
    {
        await using var db = new RecruitmentDbContext(new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseSqlServer("Server=.\\SQLEXPRESS;Database=JobTot;Trusted_Connection=True;TrustServerCertificate=True").Options);
        if (db.Database.HasPendingModelChanges()) throw new Exception("Model snapshot does not match the current EF model");
        var environment = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] }).Environment;
        var store = new JobTot.Infrastructure.Ai.AiChatHistoryStore(db, environment);
        var unknownAccount = Guid.NewGuid();
        if ((await store.ListAsync(unknownAccount, default)).Count != 0
            || await store.ReadAsync(unknownAccount, Guid.NewGuid(), null, default) is not null
            || await store.ImageAsync(unknownAccount, Guid.NewGuid(), 1, default) is not null)
            throw new Exception("Unexpected data returned for an unknown account");
        Console.WriteLine("PASS SQL Server history queries translate and execute; unknown account receives no private data.");
        Console.WriteLine("PASS EF model snapshot matches current model; migration is consistent.");
    }
    public static async Task Run()
    {
        var count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); count++; }
        var root = Path.Combine(Directory.GetCurrentDirectory(), ".artifacts", "ai-history-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production", ContentRootPath = root, Args = [] });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(CareerSearchController).Assembly);
        builder.Services.AddHttpContextAccessor(); builder.Services.AddScoped<ICurrentActor, CurrentActor>();
        // One shared database name across request scopes.
        var databaseName = "history-" + Guid.NewGuid();
        builder.Services.AddDbContext<RecruitmentDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddScoped<JobTot.Application.Persistence.IRecruitmentData, RecruitmentData>();
        builder.Services.AddCandidateAuthentication(new ConfigurationBuilder().Build(), true);
        builder.Services.AddEmployerAuthentication(); builder.Services.AddCareerSearch();
        foreach (var scheme in new[] { CandidateAuthentication.Scheme, EmployerAuthentication.Scheme })
            builder.Services.PostConfigure<JwtBearerOptions>(scheme, options => options.Events.OnTokenValidated = _ => Task.CompletedTask);
        var provider = new FixtureProvider(); builder.Services.AddSingleton<ICareerSearchProvider>(provider);
        await using var app = builder.Build();
        app.UseRouting(); app.UseAuthentication(); app.UseRateLimiter(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            const string history = "/api/ai/conversations";
            const string ask = "/api/ai/career-search";
            var issuer = new AccessTokenIssuer(app.Services.GetRequiredService<CandidateJwtSettings>());
            var owner = Guid.NewGuid(); var stranger = Guid.NewGuid();
            void Login(Guid id, string role = "Candidate") => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                issuer.Issue(role, id, Guid.NewGuid(), "Fixture", DateTimeOffset.UtcNow.AddMinutes(5)));
            Check((await client.GetAsync(history)).StatusCode == HttpStatusCode.Unauthorized, "history requires authentication");
            Login(owner);
            using var answerResponse = await client.PostAsJsonAsync(ask, new { question = "Kỹ năng backend?" });
            Check(answerResponse.IsSuccessStatusCode, "successful generation saves history");
            var answer = await answerResponse.Content.ReadFromJsonAsync<CareerSearchResponse>();
            var id = answer!.ConversationId!.Value;
            Check(id != Guid.Empty, "new conversation gets a server-generated ID");
            var summaries = await client.GetFromJsonAsync<AiConversationSummary[]>(history);
            Check(summaries!.Length == 1 && summaries[0].Id == id && summaries[0].Title == "Kỹ năng backend?", "conversation is listed under its owner");
            var imageAnswer = await client.PostAsJsonAsync(ask, new { conversationId = id, question = "Nhận xét CV", image = new { mimeType = "image/png", data = CareerImageChecks.Png } });
            Check(imageAnswer.IsSuccessStatusCode, "CV review can continue an existing conversation");
            var page = await client.GetFromJsonAsync<AiHistoryPage>($"{history}/{id}");
            Check(page!.Messages.Count == 4 && page.Messages.Select(m => m.Role).SequenceEqual(new[] { "user", "assistant", "user", "assistant" }), "question/answer order survives separate HTTP requests");
            Check(page.Messages[1].Content.Length > 16000 && page.Messages.All(m => m.State == "Completed"), "full long AI answers are restored without truncation");
            var imageMessage = page.Messages.Single(m => m.HasImage);
            var imageUrl = $"{history}/{id}/messages/{imageMessage.Id}/image";
            using var imageResponse = await client.GetAsync(imageUrl);
            Check(imageResponse.IsSuccessStatusCode && Convert.ToBase64String(await imageResponse.Content.ReadAsByteArrayAsync()) == CareerImageChecks.Png,
                "owner can restore the private CV image");
            Check(Directory.GetFiles(Path.Combine(root, "App_Data", "ai-chat")).Length == 1, "CV file is stored outside public media folders");
            Login(stranger, "Employer");
            Check((await client.GetFromJsonAsync<AiConversationSummary[]>(history))!.Length == 0, "another account cannot list owner conversations");
            Check((await client.GetAsync($"{history}/{id}")).StatusCode == HttpStatusCode.NotFound, "another account cannot open owner messages");
            Check((await client.GetAsync(imageUrl)).StatusCode == HttpStatusCode.NotFound, "another account cannot download owner CV image");
            var callsBefore = provider.Calls;
            var stolen = await client.PostAsJsonAsync(ask, new { conversationId = id, question = "stolen" });
            Check(stolen.StatusCode == HttpStatusCode.NotFound && provider.Calls == callsBefore, "forged conversation ID is rejected before generation");
            Login(owner);
            provider.Failure = new CareerSearchException("ai_provider_unavailable", "safe error", "provider_http", 503);
            using var failureResponse = await client.PostAsJsonAsync(ask, new { conversationId = id, question = "Câu hỏi bị lỗi" });
            Check(failureResponse.StatusCode == HttpStatusCode.ServiceUnavailable, "provider failure retains its HTTP error");
            page = await client.GetFromJsonAsync<AiHistoryPage>($"{history}/{id}");
            Check(page!.Messages.Count == 6 && page.Messages[^1].State == "Failed" && page.Messages[^2].Content == "Câu hỏi bị lỗi", "failed generation still preserves the user's question");
            provider.Failure = null;
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
                var start = DateTimeOffset.UtcNow;
                db.AiChatMessages.AddRange(Enumerable.Range(0, 205).Select(index => new AiChatMessage { AccountId = owner,
                    ConversationId = id, TurnId = Guid.NewGuid(), Role = "user", Content = "older-" + index, CreatedAt = start.AddTicks(index), UpdatedAt = start.AddTicks(index) }));
                await db.SaveChangesAsync();
            }
            page = await client.GetFromJsonAsync<AiHistoryPage>($"{history}/{id}");
            var older = await client.GetFromJsonAsync<AiHistoryPage>($"{history}/{id}?beforeId={page!.Messages[0].Id}");
            Check(page.HasMore && page.Messages.Count == 100 && older!.Messages.Count == 100
                && !page.Messages.Select(m => m.Id).Intersect(older.Messages.Select(m => m.Id)).Any(), "history pagination has no duplicate messages");
            // A fresh scope represents a reload/reconnect, not an in-memory UI cache.
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<IAiChatHistoryStore>();
                Check((await store.ReadAsync(owner, id, null, default))!.Messages.Count == 100, "history survives a fresh backend request scope");
                var adviceService = new AiConversationService(store, provider, new Actor(owner),
                    new AiJobLookup(scope.ServiceProvider.GetRequiredService<JobTot.Application.Persistence.IRecruitmentData>(), new Actor(owner)));
                var calls = provider.Calls;
                var advice = await adviceService.AskAsync(new() { Question = "Ngành An Ninh Mạng" }, default);
                Check(provider.Calls == calls + 1 && advice.Jobs is null && advice.Answer.Length > 16000,
                    "general career advice reaches Gemini without requiring matching jobs");
                calls = provider.Calls;
                var mixed = await adviceService.AskAsync(new() { Question = "Tư vấn an ninh mạng và gợi ý doanh nghiệp" }, default);
                Check(provider.Calls == calls + 1 && mixed.Jobs is { Count: 0 } && mixed.Answer.Length > 16000,
                    "empty job search still allows Gemini to answer the general advice portion");
                var contextDb = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
                var sector = new Industry { IndustryName = "An ninh mạng" };
                var company = new Company("Security fixture", "Public company") { Industry = sector };
                var intern = new JobPost(company.Id, "Security intern", "Public description", "Ha Noi", null, null, DateTimeOffset.UtcNow.AddDays(7));
                intern.SetDirectoryDetails("none", "intern", "internship");
                var senior = new JobPost(company.Id, "Security senior", "Public description", "Ha Noi", null, null, DateTimeOffset.UtcNow.AddDays(7));
                senior.SetDirectoryDetails("over-5", "manager", "full-time");
                contextDb.AddRange(sector, company, intern, senior);
                var marketing = new JobPost(company.Id, "Marketing intern", "Public description", "Ha Noi", null, null, DateTimeOffset.UtcNow.AddDays(7));
                marketing.SetDirectoryDetails("none", "intern", "internship");
                contextDb.Add(marketing);
                await contextDb.SaveChangesAsync();
                var followup = await adviceService.AskAsync(new() { ConversationId = advice.ConversationId,
                    Question = "Mình thấy ngành này đang bị AI thay thế nhiều liệu những bạn trẻ mới ra trường có cơ hội?" }, default);
                Check(followup.Jobs is null && followup.Answer.Length > 16000,
                    "general follow-up does not attach job cards or no-match notes");
                Check(provider.LastQuestion.Contains("Ngành An Ninh Mạng") && provider.LastQuestion.Contains("mới ra trường"),
                    "Gemini receives owned prior topic and current question to understand follow-up");
                var recruitment = await adviceService.AskAsync(new() { ConversationId = advice.ConversationId,
                    Question = "Tìm việc thực tập ngành này cho mình" }, default);
                Check(recruitment.Jobs is { Count: 1 } && recruitment.Jobs[0].Id == intern.Id,
                    "explicit recruiting follow-up resolves industry and excludes marketing within the same company");
                var ordinary = await adviceService.AskAsync(new() { ConversationId = advice.ConversationId,
                    Question = "Ngành IT đang bị ảnh hưởng bởi AI thì phải làm sao?" }, default);
                Check(ordinary.Jobs is null && !ordinary.Answer.Contains("Về việc làm trên JobTot"),
                    "earlier recruitment intent never leaks into a later ordinary advice request");
                using var cancellation = new CancellationTokenSource();
                provider.WaitForCancel = true;
                var service = new AiConversationService(store, provider, new Actor(owner));
                var pending = service.AskAsync(new() { Question = "Câu hỏi được hủy" }, cancellation.Token);
                await provider.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
                cancellation.Cancel();
                try { await pending; throw new Exception("Expected cancelled request"); }
                catch (CareerSearchException error)
                {
                    var cancelled = await store.ReadAsync(owner, error.ConversationId!.Value, null, default);
                    Check(error.Code == "ai_cancelled" && cancelled!.Messages.Count == 2 && cancelled.Messages[^1].State == "Cancelled",
                        "cancelling generation keeps the question and persists the cancellation state");
                }
            }
            Login(owner);
            Check((await client.DeleteAsync($"{history}/{id}")).StatusCode == HttpStatusCode.MethodNotAllowed, "conversation deletion endpoint is removed");
            Check((await client.GetAsync($"{history}/{id}")).IsSuccessStatusCode, "existing conversation remains available after rejected deletion");
            Console.WriteLine($"PASS {count} AI history checks; isolated InMemory database and fake Gemini only");
        }
        finally { await app.StopAsync(); }
    }
    private sealed class FixtureProvider : ICareerSearchProvider
    {
        public int Calls;
        public string LastQuestion = "";
        public CareerSearchException? Failure;
        public bool WaitForCancel;
        public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<CareerSearchResponse> SearchAsync(string question, CareerChatImage image, CancellationToken ct) => SearchAsync(question, ct);
        public async Task<CareerSearchResponse> SearchAsync(string question, CancellationToken ct)
        {
            Calls++;
            LastQuestion = question;
            if (Failure is not null) throw Failure;
            if (WaitForCancel) { Started.TrySetResult(); await Task.Delay(Timeout.Infinite, ct); }
            return new CareerSearchResponse(new string('a', 18000), [], [], [], null, "not_grounded");
        }
    }
    private sealed class Actor(Guid id) : ICurrentActor
    {
        public Guid AccountId => id;
        public Guid SessionId => Guid.Empty;
        public bool IsEmployer => false;
        public CancellationToken Cancellation => CancellationToken.None;
    }
}
