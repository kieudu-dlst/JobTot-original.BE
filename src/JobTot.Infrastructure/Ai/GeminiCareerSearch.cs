using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobTot.Application.Ai;
using Microsoft.Extensions.Configuration;

namespace JobTot.Infrastructure.Ai;

public sealed class GeminiCareerSearch(IHttpClientFactory clients, IConfiguration configuration) : ICareerSearchProvider
{
    private const string Instructions = """
        Bạn là trợ lý thông tin nghề nghiệp JobTot. Trả lời bằng tiếng Việt, ngắn gọn, tối đa khoảng 1200 từ.
        Chỉ hỗ trợ nghề nghiệp, kỹ năng, tìm việc, CV và chuẩn bị phỏng vấn. Với câu hỏi ngoài phạm vi,
        lịch sự hướng dẫn người dùng quay lại các chủ đề đó. Không có quyền truy cập dữ liệu nội bộ JobTot.
        Không có công cụ tìm kiếm hoặc truy cập web. Không khẳng định đã tra cứu hay có dữ liệu thời gian thực.
        Với thông tin cần cập nhật, nêu rõ giới hạn và hướng dẫn người dùng kiểm tra nguồn chính thức.
        Không bịa URL, việc làm hoặc mức lương.
        """;

    public async Task<CareerSearchResponse> SearchAsync(string question, CancellationToken ct)
    {
        var keys = configuration.GetSection("Gemini:ApiKeys").GetChildren()
            .Select(item => item.Value ?? "").Distinct(StringComparer.Ordinal).ToArray();
        if (keys.Length == 0 && configuration["Gemini:ApiKey"] is { } legacyKey)
            keys = [legacyKey];
        var model = configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite";
        if (keys.Length == 0 || string.IsNullOrWhiteSpace(model))
            throw Failure("ai_not_configured", "Dịch vụ AI chưa được cấu hình.");
        // Google authorization keys can contain periods. Keep header validation strict,
        // but do not reject supported keys based on the older key alphabet.
        if (keys.Length > 10 || keys.Any(key => !Regex.IsMatch(key, @"\A[A-Za-z0-9_.-]{1,512}\z")))
            throw Failure("ai_configuration", "Cấu hình truy cập dịch vụ AI không hợp lệ.", "key_validation");
        if (!Regex.IsMatch(model, "^[a-zA-Z0-9][a-zA-Z0-9._-]{0,100}$"))
            throw Failure("ai_configuration", "Cấu hình model AI không hợp lệ.", "model_validation");
        var seconds = Setting("TimeoutSeconds", 45);
        var tokens = Setting("MaxOutputTokens", 2048);
        var maxChars = Setting("MaxAnswerCharacters", 16000);
        if (seconds is < 1 or > 120 || tokens is < 128 or > 8192 || maxChars is < 100 or > 64000)
            throw Failure("ai_configuration", "Cấu hình giới hạn dịch vụ AI không hợp lệ.", "limits_validation");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(seconds));
        try
        {
            for (var index = 0; index < keys.Length; index++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post,
                    $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");
                request.Headers.Add("x-goog-api-key", keys[index]);
                request.Content = JsonContent.Create(new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = Instructions } }
                    },
                    contents = new[] { new { role = "user", parts = new[] { new { text = question } } } },
                    generationConfig = new
                    {
                        maxOutputTokens = tokens,
                        candidateCount = 1
                    },
                });
                using var response = await clients.CreateClient("gemini-career-search").SendAsync(request, deadline.Token);
                if (!response.IsSuccessStatusCode)
                {
                    var failure = await ProviderFailure(response, deadline.Token);
                    var tryNextKey = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                        or HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError
                        or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
                        || failure.ProviderReason is "API_KEY_INVALID" or "API_KEY_EXPIRED";
                    if (tryNextKey && index + 1 < keys.Length) continue;
                    throw failure;
                }
                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(deadline.Token), cancellationToken: deadline.Token);
                return Parse(document.RootElement, maxChars);
            }
            throw Failure("ai_not_configured", "Dịch vụ AI chưa được cấu hình.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw Failure("ai_timeout", "Dịch vụ AI phản hồi quá lâu. Vui lòng thử lại sau.");
        }
        catch (HttpRequestException) { throw Failure("ai_provider_error", "Không thể kết nối dịch vụ AI."); }
        catch (IOException) { throw Failure("ai_provider_error", "Kết nối dịch vụ AI bị gián đoạn."); }
        catch (JsonException) { throw Failure("ai_provider_response", "Dịch vụ AI trả về dữ liệu không hợp lệ."); }
    }

    private static CareerSearchException Failure(string code, string message, string? stage = null) => new(code, message, stage);
    private static async Task<CareerSearchException> ProviderFailure(HttpResponseMessage response, CancellationToken ct)
    {
        // Only expose known machine-readable reasons. Never return/log Google's message,
        // metadata, response body, key, project ID or other arbitrary provider text.
        string? reason = null;
        var quotas = new List<CareerQuotaViolation>();
        try
        {
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            foreach (var detail in Items(Field(Field(body.RootElement, "error"), "details")))
            {
                reason ??= Text(Field(detail, "reason")) switch
                {
                    "API_KEY_INVALID" => "API_KEY_INVALID",
                    "API_KEY_EXPIRED" => "API_KEY_EXPIRED",
                    "API_KEY_SERVICE_BLOCKED" => "API_KEY_SERVICE_BLOCKED",
                    "API_KEY_IP_ADDRESS_BLOCKED" => "API_KEY_IP_ADDRESS_BLOCKED",
                    "API_KEY_HTTP_REFERRER_BLOCKED" => "API_KEY_HTTP_REFERRER_BLOCKED",
                    "ACCESS_TOKEN_TYPE_UNSUPPORTED" => "ACCESS_TOKEN_TYPE_UNSUPPORTED",
                    "SERVICE_DISABLED" => "SERVICE_DISABLED",
                    "BILLING_DISABLED" => "BILLING_DISABLED",
                    "CONSUMER_INVALID" => "CONSUMER_INVALID",
                    _ => null,
                };
                if (response.StatusCode == HttpStatusCode.TooManyRequests
                    && Text(Field(detail, "@type")) == "type.googleapis.com/google.rpc.QuotaFailure")
                {
                    foreach (var violation in Items(Field(detail, "violations")))
                    {
                        if (quotas.Count >= 16) break;
                        // Classify only. Metric/ID/dimensions/descriptions may contain project
                        // identifiers and must never be returned as arbitrary provider strings.
                        var id = Text(Field(violation, "quotaId")) ?? "";
                        var metric = Text(Field(violation, "quotaMetric")) ?? "";
                        var search = metric.Contains("search", StringComparison.OrdinalIgnoreCase)
                            || metric.Contains("grounding", StringComparison.OrdinalIgnoreCase);
                        var kind = search ? "google_search"
                            : id.Contains("Requests", StringComparison.OrdinalIgnoreCase) && id.Contains("PerDay", StringComparison.OrdinalIgnoreCase) ? "requests_per_day"
                            : id.Contains("Tokens", StringComparison.OrdinalIgnoreCase) && id.Contains("PerMinute", StringComparison.OrdinalIgnoreCase) ? "tokens_per_minute"
                            : id.Contains("Requests", StringComparison.OrdinalIgnoreCase) && id.Contains("PerMinute", StringComparison.OrdinalIgnoreCase) ? "requests_per_minute" : "other";
                        var value = Field(violation, "quotaValue");
                        long number = 0;
                        var validLimit = value.ValueKind == JsonValueKind.Number ? value.TryGetInt64(out number) && number >= 0
                            : value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out number) && number >= 0;
                        quotas.Add(new(kind, validLimit ? number : null));
                    }
                }
            }
        }
        catch (JsonException) { /* The HTTP status is still useful for non-JSON errors. */ }
        var (code, message) = response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => ("ai_provider_quota", "Google hiện giới hạn yêu cầu hoặc đã hết hạn mức. Vui lòng thử lại sau."),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest or HttpStatusCode.NotFound =>
                ("ai_configuration", "Google từ chối yêu cầu AI. Vui lòng kiểm tra cấu hình truy cập và model."),
            _ => ("ai_provider_error", "Nhà cung cấp AI tạm thời không khả dụng."),
        };
        return new(code, message, "provider_http", (int)response.StatusCode, reason, quotas);
    }
    private int Setting(string name, int fallback) => configuration["Gemini:" + name] is not { } value ? fallback
        : int.TryParse(value, out var number) ? number : throw Failure("ai_configuration", "Cấu hình giới hạn dịch vụ AI không hợp lệ.", "limits_validation");
    private static JsonElement Field(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var field) ? field : default;
    private static string? Text(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static JsonElement[] Items(JsonElement value) => value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToArray() : [];
    private static int? Number(JsonElement value) => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;

    public static CareerSearchResponse Parse(JsonElement root, int maxChars = 16000)
    {
        var candidate = Items(Field(root, "candidates")).FirstOrDefault();
        var parts = Items(Field(Field(candidate, "content"), "parts"));
        var positions = new Dictionary<int, (int Start, string Text)>();
        var answer = new StringBuilder();
        for (var i = 0; i < parts.Length; i++)
        {
            if (Field(parts[i], "thought").ValueKind == JsonValueKind.True)
                continue;
            if (Text(Field(parts[i], "text")) is not { } text)
                continue;
            positions[i] = (answer.Length, text);
            answer.Append(text);
        }
        if (answer.Length == 0)
            throw Failure("ai_no_answer", "Không có câu trả lời phù hợp. Vui lòng diễn đạt lại câu hỏi.");
        if (answer.Length > maxChars)
            throw Failure("ai_answer_too_long", "Câu trả lời vượt giới hạn. Vui lòng thu hẹp câu hỏi.");
        var metadata = Field(candidate, "groundingMetadata");
        var sources = new List<CareerSource>();
        var byUrl = new Dictionary<string, string>(StringComparer.Ordinal);
        var chunkIds = new Dictionary<int, string>();
        var chunks = Items(Field(metadata, "groundingChunks"));
        for (var i = 0; i < chunks.Length; i++)
        {
            var web = Field(chunks[i], "web");
            var url = Text(Field(web, "uri"));
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.UserInfo.Length > 0)
                continue;
            if (!byUrl.TryGetValue(url!, out var id))
            {
                id = "s" + (sources.Count + 1);
                byUrl[url!] = id;
                sources.Add(new(id, Text(Field(web, "title")) ?? uri.Host, url!));
            }
            chunkIds[i] = id;
        }
        var citations = new List<CareerCitation>();
        foreach (var support in Items(Field(metadata, "groundingSupports")))
        {
            var segment = Field(support, "segment");
            var part = Number(Field(segment, "partIndex")) ?? 0;
            if (!positions.TryGetValue(part, out var position))
                continue;
            var start = Number(Field(segment, "startIndex")) ?? 0;
            var end = Number(Field(segment, "endIndex"));
            if (end is null || start < 0 || end <= start)
                continue;
            var bytes = Encoding.UTF8.GetBytes(position.Text);
            if (end > bytes.Length)
                continue;
            try
            {
                var utf8 = new UTF8Encoding(false, true);
                var prefix = utf8.GetString(bytes, 0, start);
                var text = utf8.GetString(bytes, start, end.Value - start);
                var reportedText = Text(Field(segment, "text"));
                if (reportedText is not null && reportedText != text)
                    continue;
                var ids = Items(Field(support, "groundingChunkIndices")).Select(Number)
                    .Where(x => x.HasValue && chunkIds.ContainsKey(x.Value)).Select(x => chunkIds[x!.Value]).Distinct().ToArray();
                if (ids.Length > 0)
                    citations.Add(new(position.Start + prefix.Length, position.Start + prefix.Length + text.Length, text, ids));
            }
            catch (DecoderFallbackException) { /* Invalid byte boundaries are not trusted citations. */ }
        }
        var queries = Items(Field(metadata, "webSearchQueries")).Select(Text).OfType<string>().Where(q => !string.IsNullOrWhiteSpace(q)).ToArray();
        var html = Text(Field(Field(metadata, "searchEntryPoint"), "renderedContent"));
        var status = sources.Count > 0 ? "grounded" : queries.Length > 0 || !string.IsNullOrWhiteSpace(html) ? "no_sources" : "not_grounded";
        return new(answer.ToString(), sources, citations, queries, html, status, IsTruncated: Text(Field(candidate, "finishReason")) == "MAX_TOKENS");
    }
}
