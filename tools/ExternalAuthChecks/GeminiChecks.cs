using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JobTot.Api;
using JobTot.Api.Controllers;
using JobTot.Application.Ai;
using JobTot.Infrastructure;
using JobTot.Infrastructure.Ai;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

public static class GeminiChecks
{
    // Explicit opt-in only: real Google request, real controller and production configuration
    // loader. Test JWT validation skips DB-backed sessions in this isolated loopback host.
    public static async Task Live(string? imagePath = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production", Args = [] });
        builder.Configuration.AddGeminiSecrets();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(CareerSearchController).Assembly);
        builder.Services.AddCandidateAuthentication(new ConfigurationBuilder().Build(), true);
        builder.Services.AddEmployerAuthentication();
        builder.Services.AddCareerSearch(persistHistory: false);
        builder.Services.PostConfigure<JwtBearerOptions>(CandidateAuthentication.Scheme,
            options => options.Events.OnTokenValidated = _ => Task.CompletedTask);
        await using var app = builder.Build();
        app.UseRouting(); app.UseAuthentication(); app.UseRateLimiter(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(60) };
            var issuer = new AccessTokenIssuer(app.Services.GetRequiredService<CandidateJwtSettings>());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                issuer.Issue("Candidate", Guid.NewGuid(), Guid.NewGuid(), "Test", DateTimeOffset.UtcNow.AddMinutes(5)));
            CareerChatImage? image = imagePath is null ? null : new(
                Path.GetExtension(imagePath).Equals(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg",
                Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath)));
            using var response = await client.PostAsJsonAsync("/api/ai/career-search",
                new CareerSearchRequest { Question = image is null ? "Backend Developer cần kỹ năng gì? Trả lời một câu ngắn."
                    : "Đọc vị trí ứng tuyển trong ảnh rồi góp ý cải thiện CV ngắn gọn. Không đoán nội dung không đọc được.", Image = image });
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Live chat HTTP {(int)response.StatusCode}; code={json.RootElement.GetProperty("code").GetString()}");
            var answer = json.RootElement.GetProperty("answer").GetString();
            if (string.IsNullOrWhiteSpace(answer)) throw new Exception("Live chat returned an empty answer");
            if (image is not null && !answer.Contains("backend", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Image review did not identify the position printed in the synthetic CV fixture");
            Console.WriteLine($"PASS live POST /api/ai/career-search: HTTP 200, {answer.Length} answer characters, no Google Search");
            if (image is not null) Console.WriteLine("PASS live image review: Gemini recognized the role shown only in the synthetic CV image.");
        }
        finally { await app.StopAsync(); }
    }

    public static async Task Run()
    {
        var count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
        var answer = "Kỹ năng 😀 .NET";
        var segment = "😀 .NET";
        var start = Encoding.UTF8.GetByteCount("Kỹ năng ");
        var fixture = JsonSerializer.Serialize(new
        {
            candidates = new[] { new {
                content = new { parts = new object[] { new { thought = true, text = "private reasoning" }, new { text = answer } } },
                groundingMetadata = new {
                    groundingChunks = new[] { new { web = new { uri = "https://example.com/a", title = "A" } }, new { web = new { uri = "https://example.com/a", title = "Duplicate" } } },
                    groundingSupports = new[] { new { segment = new { partIndex = 1, startIndex = start, endIndex = Encoding.UTF8.GetByteCount(answer), text = segment }, groundingChunkIndices = new[] { 0, 1 } } },
                    webSearchQueries = new[] { "backend skills" }, searchEntryPoint = new { renderedContent = "<div>Google suggestions</div>" }
                }
            } }
        });
        var handler = new FakeHandler(async (request, ct) =>
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Check(!body.RootElement.TryGetProperty("tools", out _), "REST request omits search tools");
            Check(!body.RootElement.GetProperty("generationConfig").TryGetProperty("maxOutputTokens", out _),
                "request does not impose an output token limit");
            Check(request.RequestUri!.AbsolutePath.EndsWith("gemini-3.5-flash-lite:generateContent") && !request.RequestUri.Query.Contains("key"), "model and API schema are consistent; no key in URL");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(fixture) };
        });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Gemini:ApiKey"] = "test-only-not-a-real-key", ["Gemini:TimeoutSeconds"] = "1" }).Build();
        var provider = new GeminiCareerSearch(new Factory(handler), configuration);
        var result = await provider.SearchAsync("Câu hỏi kiểm thử", default);
        Check(result.Answer == answer && !result.Answer.Contains("reasoning"), "answer preserves text and excludes thought parts");
        Check(result.Sources.Count == 1 && result.Citations.Single().SourceIds.SequenceEqual(new[] { "s1" }), "duplicate source indices remap to one stable ID");
        var citation = result.Citations.Single();
        Check(result.Answer[citation.StartIndex..citation.EndIndex] == segment && citation.StartIndex == "Kỹ năng ".Length, "UTF-8 byte offsets convert to JavaScript UTF-16 for Vietnamese and emoji");
        Check(result.GroundingStatus == "grounded" && result.SearchSuggestionsHtml!.Contains("Google"), "grounding and original suggestions survive mapping");
        var configuredModelCalls = 0;
        var configuredModel = new GeminiCareerSearch(new Factory(new FakeHandler(async (request, token) =>
        {
            configuredModelCalls++;
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Check(request.RequestUri!.AbsolutePath.EndsWith("gemini-2.5-flash-lite:generateContent")
                && !body.RootElement.TryGetProperty("tools", out _),
                "configured Flash Lite model is used without search tools");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(fixture) };
        })), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Gemini:ApiKey"] = "test-only-not-a-real-key", ["Gemini:Model"] = "gemini-2.5-flash-lite" }).Build());
        Check((await configuredModel.SearchAsync("test", default)).GroundingStatus == "grounded" && configuredModelCalls == 1,
            "explicit model override preserves grounded response mapping without an extra request");
        foreach (var status in new[] { 400, 401, 403, 404, 429 })
        {
            var calls = 0;
            var failover = new GeminiCareerSearch(new Factory(new FakeHandler((request, _) =>
            {
                calls++;
                Check(request.Headers.GetValues("x-goog-api-key").Single() == $"test-key-{calls}",
                    "failover uses each configured key in order");
                return Task.FromResult(new HttpResponseMessage(calls == 1 ? (HttpStatusCode)status : HttpStatusCode.OK)
                { Content = new StringContent(calls == 1 ? "{}" : fixture) });
            })), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKeys:0"] = "test-key-1", ["Gemini:ApiKeys:1"] = "test-key-2",
                ["Gemini:ApiKey"] = "ignored-legacy-key"
            }).Build());
            try { await failover.SearchAsync("test", default); }
            catch (CareerSearchException) when (status is 400 or 404) { }
            Check(calls == (status is 400 or 404 ? 1 : 2),
                $"HTTP {status} rotates only recoverable failures and ignores legacy key when pool is configured");
        }
        var transientCalls = 0;
        var recovering = new GeminiCareerSearch(new Factory(new FakeHandler((request, _) =>
        {
            transientCalls++;
            Check(request.Headers.GetValues("x-goog-api-key").Single() == "test-key-1",
                "provider overload retry keeps the same credential and model");
            return Task.FromResult(new HttpResponseMessage(transientCalls < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent(transientCalls < 3 ? "{}" : fixture) });
        })), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Gemini:ApiKeys:0"] = "test-key-1", ["Gemini:ApiKeys:1"] = "test-key-2" }).Build());
        Check((await recovering.SearchAsync("test", default)).Answer == answer && transientCalls == 3,
            "503 recovers after delayed retries and success is not replayed");
        var unavailableCalls = 0;
        var unavailable = new GeminiCareerSearch(new Factory(new FakeHandler((_, _) =>
        {
            unavailableCalls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") });
        })), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Gemini:ApiKeys:0"] = "test-key-1", ["Gemini:ApiKeys:1"] = "test-key-2" }).Build());
        try { await unavailable.SearchAsync("test", default); throw new Exception("Expected unavailable error"); }
        catch (CareerSearchException error)
        { Check(unavailableCalls == 4 && error.Code == "ai_provider_unavailable" && error.ProviderStatus == 503,
            "persistent overload stops at a shared retry budget across all credentials"); }
        using var cancelRetry = new CancellationTokenSource();
        var cancelCalls = 0;
        var cancelProvider = new GeminiCareerSearch(new Factory(new FakeHandler((_, _) =>
        {
            cancelCalls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") });
        })), configuration);
        var cancelledRetry = cancelProvider.SearchAsync("test", cancelRetry.Token);
        cancelRetry.Cancel();
        try { await cancelledRetry; throw new Exception("Expected cancelled retry"); }
        catch (CareerSearchException error)
        { Check(error.Code == "ai_cancelled" && cancelCalls == 1, "client cancellation stops retry backoff immediately without another generation"); }
        var exhaustedCalls = 0;
        var poolExhausted = new GeminiCareerSearch(new Factory(new FakeHandler((_, _) =>
        {
            exhaustedCalls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{}") });
        })), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Gemini:ApiKeys:0"] = "test-key-1", ["Gemini:ApiKeys:1"] = "test-key-2" }).Build());
        try { await poolExhausted.SearchAsync("test", default); throw new Exception("Expected exhausted pool error"); }
        catch (CareerSearchException error)
        { Check(exhaustedCalls == 2 && error.ProviderStatus == 429, "exhausted pool stops after one attempt per key and preserves quota error"); }
        using var noMetadata = JsonDocument.Parse("""{"candidates":[{"content":{"parts":[{"text":"Không có metadata"}]}}]}""");
        var chatOnly = new GeminiCareerSearch(new Factory(new FakeHandler(async (request, token) =>
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Check(!body.RootElement.TryGetProperty("tools", out _),
                "obsolete search setting cannot enable Google Search");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(noMetadata.RootElement.GetRawText()) };
        })), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Gemini:ApiKey"] = "test-key", ["Gemini:GoogleSearchEnabled"] = "true" }).Build());
        Check((await chatOnly.SearchAsync("test", default)).GroundingStatus == "not_grounded",
            "chat-only answer does not claim web grounding");
        var plain = GeminiCareerSearch.Parse(noMetadata.RootElement);
        Check(plain.GroundingStatus == "not_grounded" && plain.Sources.Count == 0, "missing metadata is valid, not proof of search");
        using var noSources = JsonDocument.Parse("""{"candidates":[{"content":{"parts":[{"text":"Tìm kiếm"}]},"groundingMetadata":{"webSearchQueries":["skills"]}}]}""");
        Check(GeminiCareerSearch.Parse(noSources.RootElement).GroundingStatus == "no_sources", "search evidence without sources has a distinct status");
        foreach (var status in new[] { 400, 401, 403, 404, 429, 500 })
        {
            var calls = 0;
            var failure = new GeminiCareerSearch(new Factory(new FakeHandler((_, _) =>
            {
                calls++; return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("sensitive provider error") });
            })), configuration);
            try { await failure.SearchAsync("test", default); throw new Exception("expected error"); }
            catch (CareerSearchException e) { Check(calls == (status == 500 ? 3 : 1) && !e.Message.Contains("sensitive") && e.ProviderStatus == status
                && e.FailureStage == "provider_http" && e.ProviderReason is null
                && e.Code == (status == 429 ? "ai_provider_quota" : status == 500 ? "ai_provider_unavailable" : "ai_configuration"), "sanitized HTTP " + status + ", bounded retries only for transient server errors"); }
        }
        foreach (var reason in new[] { "API_KEY_INVALID", "SERVICE_DISABLED", "API_KEY_SERVICE_BLOCKED", "sensitive-key-value", "__proto__" })
        {
            var errorBody = JsonSerializer.Serialize(new { error = new { message = "sensitive provider message", details = new[] {
                new { reason, metadata = new { apiKey = "sensitive-key-value", project = "sensitive-project" } } } } });
            var rejected = new GeminiCareerSearch(new Factory(new FakeHandler((_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(errorBody) }))), configuration);
            try { await rejected.SearchAsync("test", default); throw new Exception("expected Google rejection"); }
            catch (CareerSearchException e)
            {
                var acceptedReason = reason is "API_KEY_INVALID" or "SERVICE_DISABLED" or "API_KEY_SERVICE_BLOCKED";
                Check(e.ProviderStatus == 400 && e.FailureStage == "provider_http"
                    && e.ProviderReason == (acceptedReason ? reason : null) && !e.Message.Contains("sensitive"),
                    "provider diagnostic reason is allowlisted; arbitrary provider text never escapes");
            }
        }
        var slow = new GeminiCareerSearch(new Factory(new FakeHandler(async (_, ct) =>
        { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(); })), configuration);
        foreach (var sample in new[] {
            ("GenerateRequestsPerDayPerProjectPerModel-FreeTier", "requests_per_day", "0"),
            ("GenerateContentInputTokensPerModelPerMinute-FreeTier", "tokens_per_minute", "250000"),
            ("GenerateRequestsPerMinutePerProjectPerModel-FreeTier", "requests_per_minute", "15"),
            ("sensitive-project-name", "other", "invalid") })
        {
            var quotaBody = JsonSerializer.Serialize(new { error = new { message = "sensitive provider message", details = new[] {
                new Dictionary<string, object?> { ["@type"] = "type.googleapis.com/google.rpc.QuotaFailure", ["violations"] = new[] {
                    new { quotaId = sample.Item1, quotaMetric = "sensitive-project-metric", quotaValue = sample.Item3,
                        description = "sensitive description", quotaDimensions = new { project = "sensitive-project" } } } } } } });
            var exhausted = new GeminiCareerSearch(new Factory(new FakeHandler((_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent(quotaBody) }))), configuration);
            try { await exhausted.SearchAsync("test", default); throw new Exception("expected quota failure"); }
            catch (CareerSearchException e)
            {
                var quota = e.QuotaViolations.Single();
                Check(e.Code == "ai_provider_quota" && quota.Kind == sample.Item2
                    && quota.Limit == (long.TryParse(sample.Item3, out var limit) ? limit : null)
                    && !JsonSerializer.Serialize(e.QuotaViolations).Contains("sensitive"),
                    "quota diagnostics retain only a known category and numeric limit, including zero");
            }
        }
        var longAnswer = new string('a', 70000);
        using var longDocument = JsonDocument.Parse(JsonSerializer.Serialize(new
        { candidates = new[] { new { content = new { parts = new[] { new { text = longAnswer } } } } } }));
        Check(GeminiCareerSearch.Parse(longDocument.RootElement).Answer == longAnswer,
            "long answers are returned without application truncation or rejection");
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unlimited = new GeminiCareerSearch(new Factory(new FakeHandler(async (_, token) =>
        {
            waiting.SetResult();
            await release.Task.WaitAsync(token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(fixture) };
        })), configuration);
        var pending = unlimited.SearchAsync("test", default);
        await waiting.Task;
        await Task.Delay(1100);
        Check(!pending.IsCompleted, "obsolete one-second timeout setting cannot cancel a pending request");
        release.SetResult();
        Check((await pending).Answer == answer, "waiting request completes when provider responds");
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        try { await slow.SearchAsync("test", canceled.Token); throw new Exception("expected cancellation"); }
        catch (CareerSearchException e) { Check(e.Code == "ai_cancelled", "client cancellation is handled inside the provider without escaping TaskCanceledException"); }
        var missing = new GeminiCareerSearch(new Factory(handler), new ConfigurationBuilder().Build());
        try { await missing.SearchAsync("test", default); throw new Exception("expected missing config"); }
        catch (CareerSearchException e) { Check(e.Code == "ai_not_configured", "missing configuration fails only when the service is called"); }
        foreach (var testKey in new[] { "fixture-legacy_key", "fixture.authorization_key-123", new string('a', 512) })
        {
            var calls = 0;
            var keyConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Gemini:ApiKey"] = testKey }).Build();
            var keyProvider = new GeminiCareerSearch(new Factory(new FakeHandler((request, _) =>
            {
                calls++;
                Check(request.Headers.GetValues("x-goog-api-key").Single() == testKey
                    && request.Headers.Authorization is null, "synthetic credential preserved only in API key header");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(fixture) });
            })), keyConfig);
            Check((await keyProvider.SearchAsync("test", default)).Answer == answer && calls == 1,
                "supported key alphabet including periods passes local validation without retry");
        }
        foreach (var testKey in new[] { "invalid\r\nheader", "invalid\n", "invalid\r", " leading", "trailing ",
            "embedded space", "\"quoted\"", "'quoted'", "invalid;value", "invalid\u0000value", "invalid\u200bvalue", new string('a', 513) })
        {
            var calls = 0;
            var keyConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Gemini:ApiKey"] = testKey }).Build();
            var keyProvider = new GeminiCareerSearch(new Factory(new FakeHandler((_, _) =>
            {
                calls++;
                throw new Exception("Invalid credential must not reach HTTP");
            })), keyConfig);
            try { await keyProvider.SearchAsync("test", default); throw new Exception("expected invalid credential"); }
            catch (CareerSearchException e)
            {
                Check(calls == 0 && e.Code == "ai_configuration" && e.FailureStage == "key_validation"
                    && e.ProviderStatus is null && !e.Message.Contains(testKey),
                    "unsafe credential rejected before HTTP without exposing its value");
            }
        }
        foreach (var invalid in new[] { new Dictionary<string, string?> { ["Gemini:ApiKey"] = "invalid\r\nheader" },
            new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-only", ["Gemini:Model"] = "invalid/model" } })
        {
            var badConfig = new GeminiCareerSearch(new Factory(handler), new ConfigurationBuilder().AddInMemoryCollection(invalid).Build());
            try { await badConfig.SearchAsync("test", default); throw new Exception("expected configuration failure"); }
            catch (CareerSearchException e) { Check(e.Code == "ai_configuration" && !e.Message.Contains("header"), "invalid configuration is rejected safely before HTTP"); }
        }
        await CareerImageChecks.Run(Check);
        await EndpointChecks(Check, result);
        Console.WriteLine($"PASS {count} Gemini checks; fake HTTP only, no database or Google requests");
    }

    private static async Task EndpointChecks(Action<bool, string> check, CareerSearchResponse result)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production", Args = [] });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(CareerSearchController).Assembly);
        builder.Services.AddCandidateAuthentication(new ConfigurationBuilder().Build(), true);
        builder.Services.AddEmployerAuthentication(); builder.Services.AddCareerSearch(persistHistory: false);
        foreach (var scheme in new[] { CandidateAuthentication.Scheme, EmployerAuthentication.Scheme })
            builder.Services.PostConfigure<JwtBearerOptions>(scheme, options => options.Events.OnTokenValidated = _ => Task.CompletedTask);
        var fake = new BlockingProvider(result);
        builder.Services.AddSingleton<ICareerSearchProvider>(fake);
        await using var app = builder.Build();
        app.UseRouting(); app.UseAuthentication(); app.UseRateLimiter(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        const string path = "/api/ai/career-search";
        var unauthorized = await client.PostAsJsonAsync(path, new { question = "test" });
        check(unauthorized.StatusCode == HttpStatusCode.Unauthorized && (await unauthorized.Content.ReadAsStringAsync()).Contains("ai_unauthenticated"), "endpoint requires login and returns a stable 401 code");
        var issuer = new AccessTokenIssuer(app.Services.GetRequiredService<CandidateJwtSettings>());
        void Login(string role, Guid id) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issuer.Issue(role, id, Guid.NewGuid(), "Test", DateTimeOffset.UtcNow.AddMinutes(5)));
        Login("Candidate", Guid.NewGuid());
        var badImage = await client.PostAsJsonAsync(path, new { question = "review", image = new { mimeType = "image/png", data = "not-base64" } });
        check(badImage.StatusCode == HttpStatusCode.BadRequest && (await badImage.Content.ReadAsStringAsync()).Contains("ai_invalid_image"),
            "image validation failure returns a safe HTTP 400");
        var validImage = await client.PostAsJsonAsync(path, new { image = new { mimeType = "image/png", data = CareerImageChecks.Png } });
        check(validImage.IsSuccessStatusCode, "authenticated image-only request reaches chat service");
        check((await client.PostAsJsonAsync(path, new { question = " " })).StatusCode == HttpStatusCode.BadRequest, "whitespace question rejected");
        check((await client.PostAsJsonAsync(path, new { question = new string('a', 2001) })).StatusCode == HttpStatusCode.BadRequest, "oversized question rejected");
        foreach (var role in new[] { "Candidate", "Employer" })
        {
            Login(role, Guid.NewGuid());
            for (var i = 0; i < 7; i++)
                check((await client.PostAsJsonAsync(path, new { question = "skills" })).IsSuccessStatusCode,
                    role + " sequential request " + (i + 1) + " succeeds without a per-minute AI limit");
        }
        Login("Candidate", Guid.NewGuid());
        fake.Failure = new CareerSearchException("ai_configuration", "Google từ chối yêu cầu AI.",
            "provider_http", 403, "API_KEY_SERVICE_BLOCKED");
        var providerRejection = await client.PostAsJsonAsync(path, new { question = "skills" });
        using (var errorJson = JsonDocument.Parse(await providerRejection.Content.ReadAsStringAsync()))
        {
            var error = errorJson.RootElement;
            check(providerRejection.StatusCode == HttpStatusCode.ServiceUnavailable
                && error.GetProperty("failureStage").GetString() == "provider_http"
                && error.GetProperty("providerStatus").GetInt32() == 403
                && error.GetProperty("providerReason").GetString() == "API_KEY_SERVICE_BLOCKED",
                "endpoint exposes only structured safe provider diagnostics alongside the stable error code");
        }
        fake.Failure = new CareerSearchException("ai_provider_quota", "Google đang giới hạn yêu cầu.",
            "provider_http", 429, quotaViolations: [new("requests_per_day", 0)]);
        var exhaustedResponse = await client.PostAsJsonAsync(path, new { question = "skills" });
        using (var errorJson = JsonDocument.Parse(await exhaustedResponse.Content.ReadAsStringAsync()))
        {
            var error = errorJson.RootElement;
            check(exhaustedResponse.StatusCode == HttpStatusCode.ServiceUnavailable
                && error.GetProperty("providerStatus").GetInt32() == 429
                && error.GetProperty("quotaViolations")[0].GetProperty("kind").GetString() == "requests_per_day"
                && error.GetProperty("quotaViolations")[0].GetProperty("limit").GetInt64() == 0,
                "endpoint serializes safe Google quota details for frontend zero-limit handling");
        }
        fake.Failure = new CareerSearchException("ai_provider_unavailable", "Trợ lý AI đang bận.", "provider_http", 503);
        var unavailableResponse = await client.PostAsJsonAsync(path, new { question = "skills" });
        check(unavailableResponse.StatusCode == HttpStatusCode.ServiceUnavailable
            && unavailableResponse.Headers.RetryAfter?.Delta == TimeSpan.FromSeconds(5)
            && (await unavailableResponse.Content.ReadAsStringAsync()).Contains("ai_provider_unavailable"),
            "provider overload returns HTTP 503 and safe retry guidance rather than a generic 502");
        fake.Failure = null;
        fake.Block = true;
        var running = new List<Task<HttpResponseMessage>>();
        for (var i = 0; i < 4; i++) { Login("Candidate", Guid.NewGuid()); running.Add(client.PostAsJsonAsync(path, new { question = "skills" })); }
        await fake.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Login("Candidate", Guid.NewGuid());
        var busy = await client.PostAsJsonAsync(path, new { question = "skills" });
        check(busy.StatusCode == HttpStatusCode.TooManyRequests && (await busy.Content.ReadAsStringAsync()).Contains("ai_busy"), "fifth concurrent AI call is rejected without invoking provider");
        fake.Release.TrySetResult(); await Task.WhenAll(running);
        await app.StopAsync();
    }
    private sealed class BlockingProvider(CareerSearchResponse result) : ICareerSearchProvider
    {
        public Task<CareerSearchResponse> SearchAsync(string question, CareerChatImage image, CancellationToken ct) => SearchAsync(question, ct);
        public bool Block;
        public CareerSearchException? Failure;
        private int active;
        public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<CareerSearchResponse> SearchAsync(string question, CancellationToken ct)
        {
            if (Failure is not null) throw Failure;
            if (Block) { if (Interlocked.Increment(ref active) == 4) Started.TrySetResult(); await Release.Task.WaitAsync(ct); }
            return result;
        }
    }
    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => action(request, ct);
    }
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false) { Timeout = Timeout.InfiniteTimeSpan };
    }
}
