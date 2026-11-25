using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Ai;

public sealed class CareerSearchRequest
{
    [StringLength(2000, ErrorMessage = "Câu hỏi tối đa 2000 ký tự.")]
    public string Question { get; init; } = "";
    public CareerChatImage? Image { get; init; }
    public Guid? ConversationId { get; init; }
}
public sealed record CareerChatImage(string MimeType, string Data);
public sealed record CareerSource(string Id, string Title, string Url);
// Offsets refer to UTF-16 code units in Answer, as used by JavaScript String.slice.
public sealed record CareerCitation(int StartIndex, int EndIndex, string Text, IReadOnlyList<string> SourceIds);
public sealed record CareerSearchResponse(string Answer, IReadOnlyList<CareerSource> Sources,
    IReadOnlyList<CareerCitation> Citations, IReadOnlyList<string> SearchQueries,
    string? SearchSuggestionsHtml, string GroundingStatus, string CitationIndexUnit = "utf16", bool IsTruncated = false,
    Guid? ConversationId = null, IReadOnlyList<AiPublicJob>? Jobs = null);
public interface ICareerSearchProvider
{
    Task<CareerSearchResponse> SearchAsync(string question, CancellationToken ct);
    Task<CareerSearchResponse> SearchGroundedAsync(string question, CareerChatImage? image, IReadOnlyList<AiPublicJob> jobs, CancellationToken ct)
        => image is null ? SearchAsync(question, ct) : SearchAsync(question, image, ct);
    Task<CareerSearchResponse> SearchAsync(string question, CareerChatImage image, CancellationToken ct)
        => throw new CareerSearchException("ai_invalid_image", "Dịch vụ chưa hỗ trợ ảnh CV.");
}
public sealed class CareerSearchService(ICareerSearchProvider provider)
{
    public Task<CareerSearchResponse> SearchAsync(CareerSearchRequest request, CancellationToken ct)
    {
        var question = QuestionFor(request);
        return request.Image is null ? provider.SearchAsync(question, ct) : provider.SearchAsync(question, request.Image, ct);
    }
    public static string QuestionFor(CareerSearchRequest request)
    {
        var text = request.Question ?? "";
        if ((string.IsNullOrWhiteSpace(text) && request.Image is null) || text.Length > 2000)
            throw new CareerSearchException("ai_invalid_question", "Vui lòng nhập câu hỏi từ 1 đến 2000 ký tự.");
        if (request.Image is null) return text;
        CareerImageValidation.Validate(request.Image);
        var question = string.IsNullOrWhiteSpace(text)
            ? "Hãy nhận xét CV trong ảnh: điểm mạnh, điểm cần cải thiện và đề xuất chỉnh sửa cụ thể. Không nhắc lại thông tin liên hệ cá nhân."
            : text;
        return question;
    }
}
public sealed record CareerQuotaViolation(string Kind, long? Limit);
public sealed class CareerSearchException(string code, string message, string? failureStage = null,
    int? providerStatus = null, string? providerReason = null,
    IReadOnlyList<CareerQuotaViolation>? quotaViolations = null) : Exception(message)
{
    public string Code { get; } = code;
    public string? FailureStage { get; } = failureStage;
    public int? ProviderStatus { get; } = providerStatus;
    public string? ProviderReason { get; } = providerReason;
    public IReadOnlyList<CareerQuotaViolation> QuotaViolations { get; } = quotaViolations ?? [];
    public Guid? ConversationId { get; set; }
}
