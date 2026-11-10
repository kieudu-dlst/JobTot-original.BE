using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Administration;

public sealed class AdminListQuery
{
    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int Size { get; set; } = 20;

    [StringLength(200)]
    public string? Keyword { get; set; }

    [RegularExpression("^(Active|Inactive)$")]
    public string? Status { get; set; }
}

public sealed record AdminPage<T>(IReadOnlyList<T> Items, int Total, int Page, int Size);

public sealed record AdminCompanyRow(
    Guid Id,
    string Name,
    string? LegalName,
    string? TaxCode,
    string Email,
    string? Phone,
    string? WebsiteUrl,
    string? AddressLine,
    string? Industry,
    string VerificationStatus,
    string Status,
    DateTimeOffset CreatedDate
);

public sealed record AdminUserRow(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    string? AccountType,
    string Status,
    DateTimeOffset CreatedDate
);

public sealed record AdminIndustryRow(Guid Id, string Name, string Status, int CompanyCount);

public sealed class AdminStatusInput
{
    [Required]
    public bool? Active { get; init; }
}

public sealed class AdminVerificationInput
{
    [Required, RegularExpression("^(Pending|Verified|Rejected)$")]
    public string VerificationStatus { get; init; } = "Pending";
}

public sealed class AdminPasswordInput
{
    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = "";
}

public sealed class AdminIndustryInput
{
    [Required, StringLength(200)]
    public string Name { get; init; } = "";
}
