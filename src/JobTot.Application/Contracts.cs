using System.ComponentModel.DataAnnotations;

namespace JobTot.Application;

public sealed record CreateCompanyRequest(
    [Required, StringLength(200)] string Name,
    [Required, StringLength(4000)] string Description
);

public sealed record CompanyDto(Guid Id, string Name, string Description);

public sealed record SaveJobRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(10000)] string Description,
    [Required, StringLength(200)] string Location,
    decimal? SalaryMin,
    decimal? SalaryMax,
    DateTimeOffset ExpiresAt
)
{
    [StringLength(10000)]
    public string? Requirements { get; init; }

    [StringLength(4000)]
    public string? HiringProcess { get; init; }

    [Range(1, 10000)]
    public int? Vacancies { get; init; }

    [StringLength(100)]
    public string? GenderRequirement { get; init; }

    [StringLength(100)]
    public string? AgeRequirement { get; init; }

    [StringLength(200)]
    public string? RecruiterName { get; init; }

    [StringLength(200)]
    public string? RecruiterTitle { get; init; }

    [StringLength(254), EmailAddress]
    public string? RecruiterEmail { get; init; }
    public Guid? ProvinceId { get; init; }

    [RegularExpression("^(none|under-1|1-3|3-5|over-5)$")]
    public string? ExperienceLevel { get; init; }

    [RegularExpression("^(intern|employee|supervisor|manager|director)$")]
    public string? PositionLevel { get; init; }

    [RegularExpression("^(full-time|part-time|internship|remote|flexible)$")]
    public string? EmploymentType { get; init; }

    [Required, RegularExpression("^(monthly|daily|hourly)$")]
    public string SalaryPeriod { get; init; } = "monthly";

    [StringLength(200)]
    public string? WorkSchedule { get; init; }

    [StringLength(200)]
    public string? SalaryNote { get; init; }

    [StringLength(1000)]
    public string? Benefits { get; init; }
}

public sealed record CreateJobRequest(Guid CompanyId, [Required] SaveJobRequest Job);

public sealed record JobDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    string Description,
    string Location,
    decimal? SalaryMin,
    decimal? SalaryMax,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsClosed
)
{
    [StringLength(10000)]
    public string? Requirements { get; init; }

    [StringLength(4000)]
    public string? HiringProcess { get; init; }

    [Range(1, 10000)]
    public int? Vacancies { get; init; }

    [StringLength(100)]
    public string? GenderRequirement { get; init; }

    [StringLength(100)]
    public string? AgeRequirement { get; init; }

    [StringLength(200)]
    public string? RecruiterName { get; init; }

    [StringLength(200)]
    public string? RecruiterTitle { get; init; }

    [StringLength(254), EmailAddress]
    public string? RecruiterEmail { get; init; }
    public Guid? ProvinceId { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanyLogoUrl { get; init; }
    public bool IsVerified { get; init; }
    public string? Industry { get; init; }
    public string? ExperienceLevel { get; init; }
    public string? PositionLevel { get; init; }
    public string? EmploymentType { get; init; }
    public string SalaryPeriod { get; init; } = "monthly";
    public string? WorkSchedule { get; init; }
    public string? SalaryNote { get; init; }
    public string? Benefits { get; init; }
}

public sealed class JobSearch : IValidatableObject
{
    [StringLength(200)]
    public string? Keyword { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? CompanyId { get; set; }
    public bool IncludeClosed { get; set; }
    public Guid? IndustryId { get; set; }
    public bool VerifiedOnly { get; set; }

    [RegularExpression("^(under-10|10-15|15-20|20-30|over-30|negotiable)$")]
    public string? SalaryBand { get; set; }

    [MaxLength(5)]
    public string[] ExperienceLevels { get; set; } = [];

    [MaxLength(5)]
    public string[] PositionLevels { get; set; } = [];

    [MaxLength(5)]
    public string[] EmploymentTypes { get; set; } = [];

    [MaxLength(1000)]
    public Guid[] JobIds { get; set; } = [];

    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (
            ExperienceLevels.Any(x =>
                !new[] { "none", "under-1", "1-3", "3-5", "over-5" }.Contains(x)
            )
        )
            yield return new("Invalid experience level.", [nameof(ExperienceLevels)]);
        if (
            PositionLevels.Any(x =>
                !new[] { "intern", "employee", "supervisor", "manager", "director" }.Contains(x)
            )
        )
            yield return new("Invalid position level.", [nameof(PositionLevels)]);
        if (
            EmploymentTypes.Any(x =>
                !new[] { "full-time", "part-time", "internship", "remote", "flexible" }.Contains(x)
            )
        )
            yield return new("Invalid employment type.", [nameof(EmploymentTypes)]);
    }
}

public sealed record JobFilterOption(string Value, string Label, int Count);

public sealed record JobDirectoryMetadata(
    int TotalJobs,
    int TotalCompanies,
    IReadOnlyList<JobFilterOption> SalaryBands,
    IReadOnlyList<JobFilterOption> ExperienceLevels,
    IReadOnlyList<JobFilterOption> PositionLevels,
    IReadOnlyList<JobFilterOption> EmploymentTypes
);

public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record SubmitJobApplicationRequest(
    Guid CvId,
    [Required, StringLength(200)] string FullName,
    [Required, RegularExpression(@"^\+?[0-9 ()-]{8,30}$")] string Phone,
    [StringLength(4000)] string? CoverLetter
);

public sealed record JobSubmissionDto(
    Guid Id,
    Guid JobId,
    Guid CvId,
    string CvTitle,
    DateTimeOffset AppliedAt,
    string Status
);

public sealed class JobApplicationConflictException(string message) : Exception(message);

public sealed class NotFoundException(string message) : Exception(message);
