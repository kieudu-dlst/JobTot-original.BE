using System.ComponentModel.DataAnnotations;
using JobTot.Application.Authentication;

namespace JobTot.Application.Profiles;

public sealed class CandidateProfileUpdateRequest : IValidatableObject
{
    [Required]
    public CandidateProfileDocument Profile { get; init; } = null!;

    // Omitted/null preserves the current number; an empty string explicitly clears it.
    public string? Phone { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        string? error = null;
        try
        {
            CandidateProfilePhone.Normalize(Phone);
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
        }
        if (error is not null)
            yield return new(error, [nameof(Phone)]);
    }
}

public static class CandidateProfilePhone
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (value.Count(c => c is >= '0' and <= '9') > 15)
            throw new ArgumentException("Số điện thoại chỉ được chứa tối đa 15 chữ số.");
        if (!CandidateIdentifier.TryParse(value, out var identifier) || identifier?.Phone is null)
            throw new ArgumentException(
                "Nhập số điện thoại di động Việt Nam hợp lệ (ví dụ: 0356520207 hoặc +84356520207)."
            );
        return identifier.Phone;
    }
}

public sealed class CandidatePhoneConflictException()
    : Exception("Số điện thoại đã được sử dụng bởi tài khoản khác.");
