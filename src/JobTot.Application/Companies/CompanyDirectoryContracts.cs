using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Companies;

public sealed class CompanySearch : IValidatableObject
{
    public Guid? CompanyId { get; set; }

    [StringLength(200)]
    public string? Keyword { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? IndustryId { get; set; }

    [Range(1, int.MaxValue)]
    public int? CompanySizeMin { get; set; }

    [Range(1, int.MaxValue)]
    public int? CompanySizeMax { get; set; }
    public bool VerifiedOnly { get; set; }
    public bool HiringOnly { get; set; }

    [RegularExpression("^(relevant|jobs|name)$")]
    public string Sort { get; set; } = "relevant";

    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CompanySizeMin.HasValue && CompanySizeMax.HasValue && CompanySizeMin > CompanySizeMax)
            yield return new ValidationResult(
                "CompanySizeMax must be greater than or equal to CompanySizeMin.",
                [nameof(CompanySizeMin), nameof(CompanySizeMax)]
            );
    }
}

public sealed record CompanyListingDto(
    Guid Id,
    string Name,
    string Description,
    string? Industry,
    string? Province,
    string? Address,
    int? SizeMin,
    int? SizeMax,
    string? LogoUrl,
    string? CoverUrl,
    bool IsVerified,
    int OpenJobCount,
    IReadOnlyList<string> HiringTitles
)
{
    public EmployerCompanyBrand? Brand { get; init; }
}

public sealed record CompanyFilterOption(Guid Id, string Name, int Count);

public sealed record CompanyDirectoryStats(
    int TotalCompanies,
    int VerifiedCompanies,
    int HiringCompanies,
    int OpenJobs
);

public sealed record CompanyDirectoryMetadata(
    CompanyDirectoryStats Stats,
    IReadOnlyList<CompanyFilterOption> Provinces,
    IReadOnlyList<CompanyFilterOption> Industries
);

public interface ICompanyDirectoryRepository
{
    Task<PageResult<CompanyListingDto>> SearchAsync(CompanySearch search, CancellationToken ct);
    Task<CompanyDirectoryMetadata> MetadataAsync(CancellationToken ct);
    Task<IReadOnlyList<Guid>> FollowedAsync(Guid accountId, CancellationToken ct);
    Task SetFollowedAsync(Guid accountId, Guid companyId, bool followed, CancellationToken ct);
}
