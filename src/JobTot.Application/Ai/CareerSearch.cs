using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Ai;

public sealed class CareerSearchRequest
{
    [Required(ErrorMessage = "Vui lòng nhập câu hỏi."), StringLength(2000, ErrorMessage = "Câu hỏi tối đa 2000 ký tự.")]
    public string Question { get; init; } = "";
}
public sealed record CareerSource(string Id, string Title, string Url);
// Offsets refer to UTF-16 code units in Answer, as used by JavaScript String.slice.
public sealed record CareerCitation(int StartIndex, int EndIndex, string Text, IReadOnlyList<string> SourceIds);
public sealed record CareerSearchResponse(string Answer, IReadOnlyList<CareerSource> Sources,
    IReadOnlyList<CareerCitation> Citations, IReadOnlyList<string> SearchQueries,
    string? SearchSuggestionsHtml, string GroundingStatus, string CitationIndexUnit = "utf16", bool IsTruncated = false);
public interface ICareerSearchProvider
{
    Task<CareerSearchResponse> SearchAsync(string question, CancellationToken ct);
}
public sealed class CareerSearchService(ICareerSearchProvider provider)
{
    public Task<CareerSearchResponse> SearchAsync(CareerSearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 2000)
            throw new CareerSearchException("ai_invalid_question", "Vui lòng nhập câu hỏi từ 1 đến 2000 ký tự.");
        return provider.SearchAsync(request.Question, ct);
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
}
