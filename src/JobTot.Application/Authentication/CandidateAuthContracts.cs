using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Authentication;

public sealed class CandidateRegisterRequest
{
    [StringLength(5000)]
    public string? EmailVerificationToken { get; init; }

    [Required, StringLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required, StringLength(320)]
    public string EmailOrPhone { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed class CandidateLoginRequest
{
    [Required, StringLength(320)]
    public string EmailOrPhone { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}

public sealed record CandidateAccountDto(
    Guid Id,
    Guid CandidateProfileId,
    string FullName,
    string? Email,
    string? Phone,
    string AccountType
)
{
    public string? AvatarUrl { get; init; }
}

public sealed record CandidateSessionDto(
    CandidateAccountDto Account,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshExpiresAt
);

public sealed class CandidateRefreshRequest
{
    [Required, StringLength(256)]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required, StringLength(8192)]
    public string Token { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed class DuplicateAccountException()
    : Exception("Email hoặc số điện thoại đã được đăng ký.");

public sealed class InvalidCredentialsException()
    : Exception("Email/số điện thoại hoặc mật khẩu không đúng, hoặc tài khoản không khả dụng.");
