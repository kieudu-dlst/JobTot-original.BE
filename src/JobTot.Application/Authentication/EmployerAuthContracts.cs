using System.ComponentModel.DataAnnotations;
using JobTot.Domain.Entities;

namespace JobTot.Application.Authentication;

public sealed class EmployerRegisterRequest
{
    [StringLength(5000)]
    public string? EmailVerificationToken { get; init; }

    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; init; } = "";

    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = "";

    [Required, StringLength(32)]
    public string WorkPhone { get; init; } = "";

    [Required, StringLength(200, MinimumLength = 2)]
    public string CompanyName { get; init; } = "";

    [Required, RegularExpression(@"^[0-9]{10}(-[0-9]{3})?$")]
    public string TaxCode { get; init; } = "";
    public Guid HeadOfficeProvinceId { get; init; }

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = "";

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = "";

    [Range(typeof(bool), "true", "true")]
    public bool AcceptTerms { get; init; }

    [Range(typeof(bool), "true", "true")]
    public bool RecruitmentCommitment { get; init; }
}

public sealed class EmployerLoginRequest
{
    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = "";

    [Required, StringLength(128)]
    public string Password { get; init; } = "";
    public bool RememberMe { get; init; }
}

public sealed record EmployerAccountDto(
    Guid Id,
    Guid RecruiterProfileId,
    string FullName,
    string? Email,
    string? Phone,
    string AccountType,
    Guid CompanyId,
    string CompanyName,
    string? TaxCode,
    Guid? HeadOfficeProvinceId,
    string? VerificationStatus
);

public sealed record EmployerSessionDto(
    EmployerAccountDto Account,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshExpiresAt
);

public interface IEmployerAuthRepository
{
    Task<Account?> FindAsync(CandidateIdentifier identifier, CancellationToken ct);
    Task<Account?> GetAsync(Guid id, CancellationToken ct);
    Task<EmployerRegistrationStatus> AddAsync(Account account, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<bool> ResetPasswordAsync(
        Guid id,
        string expectedHash,
        string newHash,
        CancellationToken ct
    );
}

public enum EmployerRegistrationStatus
{
    Created,
    DuplicateAccount,
    CompanyConflict,
}

public sealed record EmployerRegistrationResult(
    EmployerRegistrationStatus Status,
    EmployerAccountDto? Account = null
);

public sealed class EmployerCompanyConflictException()
    : Exception(
        "Mã số thuế đã được đăng ký. Vui lòng liên hệ quản trị viên doanh nghiệp để được thêm tài khoản HR."
    );
