using System.Net;
using System.Text.Json;
using JobTot.Application.Ai;
using JobTot.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;

public static class CareerImageChecks
{
    public const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jK1sAAAAASUVORK5CYII=";
    public static async Task Run(Action<bool, string> check)
    {
        var jpeg = "/9j/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/2wBDAQcHBwoIChMKChMoGhYaKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCj/wAARCAACAAIDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAj/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFAEBAAAAAAAAAAAAAAAAAAAAAP/EABQRAQAAAAAAAAAAAAAAAAAAAAD/2gAMAwEAAhEDEQA/AKpAB//Z";
        var calls = 0;
        var provider = new GeminiCareerSearch(new Factory(new Handler(async (request, ct) =>
        {
            calls++;
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            var parts = body.RootElement.GetProperty("contents")[0].GetProperty("parts");
            check(parts.GetArrayLength() == 2 && !string.IsNullOrWhiteSpace(parts[0].GetProperty("text").GetString()),
                "CV image request includes a question or default review prompt");
            check(parts[1].GetProperty("inlineData").GetProperty("data").GetString() is Png ||
                parts[1].GetProperty("inlineData").GetProperty("data").GetString() == jpeg,
                "CV bytes are passed inline to Gemini without an external file URL");
            check(!body.RootElement.TryGetProperty("tools", out _) && !body.RootElement.GetProperty("generationConfig").TryGetProperty("maxOutputTokens", out _),
                "image review does not enable Google Search or add an output length limit");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"candidates":[{"content":{"parts":[{"text":"Nhận xét CV"}]}}]}""") };
        })), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-only" }).Build());
        var service = new CareerSearchService(provider);
        foreach (var image in new[] { new CareerChatImage("image/png", Png), new CareerChatImage("image/jpeg", jpeg) })
            check((await service.SearchAsync(new() { Image = image }, default)).Answer == "Nhận xét CV",
                "image-only PNG/JPEG requests return a CV review");
        foreach (var image in new[]
        {
            new CareerChatImage("image/svg+xml", Png), new CareerChatImage("image/jpeg", Png),
            new CareerChatImage("image/png", "not base64"), new CareerChatImage("image/png", ""),
            new CareerChatImage("image/png", Convert.ToBase64String(new byte[CareerImageValidation.MaxBytes + 1])),
        })
        {
            try { await service.SearchAsync(new() { Question = "review", Image = image }, default); throw new Exception("Expected invalid image"); }
            catch (CareerSearchException error) { check(error.Code == "ai_invalid_image", "invalid, disguised or oversized CV image is rejected before Google"); }
        }
        check(calls == 2, "invalid images do not invoke Gemini");
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct); }
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, false) { Timeout = Timeout.InfiniteTimeSpan }; }
}
