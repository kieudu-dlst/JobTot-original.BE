using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Profiles;

public sealed class CandidateProfileDocument : IValidatableObject
{
    [StringLength(2800000)]
    public string? AvatarUrl { get; set; }

    [Required, StringLength(200)]
    public string FullName { get; set; } = "";

    [StringLength(200)]
    public string Headline { get; set; } = "";

    [StringLength(200)]
    public string Location { get; set; } = "";
    public Guid? ProvinceId { get; set; }
    public DateOnly? BirthDate { get; set; }

    [StringLength(30), RegularExpression("^(Nam|Nữ|Khác)$")]
    public string Gender { get; set; } = "";

    [StringLength(10000)]
    public string Summary { get; set; } = "";

    [Required, RegularExpression("^(looking|considering|paused)$")]
    public string ReadyStatus { get; set; } = "looking";

    [Range(0, 1000000000)]
    public decimal? ExpectedSalaryMin { get; set; }

    [Range(0, 1000000000)]
    public decimal? ExpectedSalaryMax { get; set; }

    [StringLength(200)]
    public string DesiredPosition { get; set; } = "";

    [StringLength(100)]
    public string WorkType { get; set; } = "";

    [StringLength(200)]
    public string DesiredLocation { get; set; } = "";
    public Guid? DesiredProvinceId { get; set; }

    [MaxLength(50)]
    public List<ProfileEntry> Experiences { get; set; } = [];

    [MaxLength(100)]
    public List<ProfileEntry> Skills { get; set; } = [];

    [MaxLength(30)]
    public List<ProfileEntry> Education { get; set; } = [];

    [MaxLength(50)]
    public List<ProfileEntry> Certificates { get; set; } = [];

    [MaxLength(50)]
    public List<ProfileEntry> Projects { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(FullName))
            yield return new("Họ tên không được để trống.", [nameof(FullName)]);
        if (ExpectedSalaryMin > ExpectedSalaryMax)
            yield return new(
                "Lương tối đa phải lớn hơn hoặc bằng lương tối thiểu.",
                [nameof(ExpectedSalaryMax)]
            );
        if (BirthDate > DateOnly.FromDateTime(DateTime.UtcNow))
            yield return new("Ngày sinh không hợp lệ.", [nameof(BirthDate)]);
        if (new[] { Experiences, Skills, Education, Certificates, Projects }.Any(x => x is null))
            yield return new("Danh sách hồ sơ không được null.");
    }
}

public sealed class ProfileEntry
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [StringLength(200)]
    public string Organization { get; set; } = "";

    [StringLength(100)]
    public string Period { get; set; } = "";

    [StringLength(10000)]
    public string Description { get; set; } = "";

    [StringLength(1000)]
    public string Tags { get; set; } = "";
}

public record CandidateProfileActivity(
    int Views,
    int Invitations,
    IReadOnlyList<CandidateProfileVisitor> Visitors
);

public record CandidateProfileVisitor(Guid CompanyId, string Name, DateTimeOffset ViewedAt);

public record CandidateProfileResponse(
    CandidateProfileDocument Profile,
    string? Email,
    string? Phone,
    IReadOnlyList<ProfileCv> Cvs
)
{
    public CandidateProfileActivity? Activity { get; init; }
}

public record ProfileCv(Guid Id, string Title, bool IsDefault, bool IsOnline = false);

public interface ICandidateProfileRepository
{
    Task<CandidateProfileResponse?> GetAsync(Guid accountId, CancellationToken ct);
    Task<CandidateProfileResponse?> GetSummaryAsync(Guid accountId, CancellationToken ct) =>
        GetAsync(accountId, ct);
    Task<CandidateProfileResponse?> SaveAsync(
        Guid accountId,
        CandidateProfileDocument document,
        CancellationToken ct
    );
    Task<CandidateProfileResponse?> SetAvatarAsync(
        Guid accountId,
        string? avatarUrl,
        CancellationToken ct
    );
    Task<ProfileCv?> UploadCvAsync(
        Guid accountId,
        string name,
        byte[] content,
        CancellationToken ct
    );
    Task<IReadOnlyList<ProfileCv>?> SetCvDefaultAsync(
        Guid accountId,
        Guid cvId,
        bool isDefault,
        CancellationToken ct
    );
    Task<(string Name, byte[] Content)?> GetCvAsync(
        Guid accountId,
        Guid cvId,
        CancellationToken ct
    );
}
