using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Cvs;

public sealed class CvSection
{
    [Required, StringLength(80)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, StringLength(100)]
    public string Title { get; set; } = "Mục CV";

    [StringLength(12000)]
    public string Body { get; set; } = "";
    public bool Visible { get; set; } = true;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
}

public sealed class CvDocument : IValidatableObject
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = "NGUYỄN HOÀNG NAM";

    [StringLength(200)]
    public string Headline { get; set; } = "Vị trí ứng tuyển của bạn";

    [StringLength(320)]
    public string Email { get; set; } = "email@example.com";

    [StringLength(50)]
    public string Phone { get; set; } = "0900 000 000";

    [StringLength(300)]
    public string Address { get; set; } = "Hà Nội";

    [StringLength(300)]
    public string Website { get; set; } = "";

    [StringLength(300000)]
    public string? AvatarDataUri { get; set; }

    [RegularExpression("^(one-column|two-column|sidebar|bento)$")]
    public string Layout { get; set; } = "one-column";

    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string Color { get; set; } = "#ff5722";

    [RegularExpression("^(Roboto|Inter|Arial|Georgia)$")]
    public string Font { get; set; } = "Roboto";

    [Range(10, 14)]
    public decimal FontSize { get; set; } = 10.5m;

    [Range(10, 20)]
    public int Margin { get; set; } = 15;

    [MinLength(1), MaxLength(20)]
    public List<CvSection> Sections { get; set; } =
        [
            new()
            {
                Id = "summary",
                Title = "TÓM TẮT CHUYÊN MÔN",
                Body =
                    "Giới thiệu kinh nghiệm, thế mạnh chuyên môn và mục tiêu nghề nghiệp của bạn.",
            },
            new()
            {
                Id = "experience",
                Title = "KINH NGHIỆM LÀM VIỆC",
                Body =
                    "Tên công ty • Chức danh • Thời gian\n• Mô tả công việc và trách nhiệm chính.\n• Thành tựu nổi bật, kèm số liệu cụ thể.",
            },
            new()
            {
                Id = "skills",
                Title = "KỸ NĂNG CHUYÊN MÔN",
                Body = "Kỹ năng chuyên môn\nCông cụ và công nghệ\nNgoại ngữ",
            },
            new()
            {
                Id = "education",
                Title = "HỌC VẤN & CHỨNG CHỈ",
                Body = "Tên trường • Chuyên ngành • Thời gian\nBằng cấp hoặc chứng chỉ liên quan.",
            },
        ];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!string.IsNullOrEmpty(AvatarDataUri))
        {
            var prefix =
                AvatarDataUri.StartsWith("data:image/png;base64,", StringComparison.Ordinal)
                    ? "data:image/png;base64,"
                : AvatarDataUri.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal)
                    ? "data:image/jpeg;base64,"
                : null;
            var bytes = new byte[225000];
            var valid =
                prefix is not null
                && Convert.TryFromBase64String(
                    AvatarDataUri[prefix.Length..],
                    bytes,
                    out var length
                )
                && length <= 200 * 1024
                && (
                    prefix.Contains("png")
                        ? length >= 8
                            && bytes
                                .AsSpan(0, 8)
                                .SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                        : length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255
                );
            if (!valid)
                yield return new ValidationResult(
                    "Ảnh CV phải là PNG/JPG, tối đa 200 KB.",
                    [nameof(AvatarDataUri)]
                );
        }
        if (
            Sections is null
            || Sections.Any(s => s is null)
            || Sections.Select(s => s.Id).Distinct().Count() != Sections.Count
        )
            yield return new ValidationResult(
                "Các mục CV phải có mã riêng, không trùng nhau.",
                [nameof(Sections)]
            );
    }
}

public sealed class CvTemplateWrite
{
    [Required, StringLength(200)]
    public string Name { get; set; } = "Mẫu CV mới";

    [StringLength(1000)]
    public string Description { get; set; } = "";

    [Required, StringLength(100)]
    public string Category { get; set; } = "Chuyên nghiệp";

    [RegularExpression("^(draft|published|hidden)$")]
    public string PublicationState { get; set; } = "draft";

    [Required]
    public CvDocument Document { get; set; } = new();
    public Guid Version { get; set; }
}

public sealed class CvWrite
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "CV của tôi";

    [Required]
    public CvDocument Document { get; set; } = new();
    public Guid Version { get; set; }
}

public sealed record CvTemplateDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string PublicationState,
    Guid Version,
    CvDocument Document
);

public sealed record OnlineCvDto(
    Guid Id,
    string Title,
    Guid Version,
    bool IsDefault,
    CvDocument Document
);

public sealed record OnlineCvListDto(
    Guid Id,
    string Title,
    Guid Version,
    bool IsDefault,
    DateTimeOffset UpdatedAt
);

public sealed class CvVersionConflictException : Exception;

public interface ICvRepository
{
    Task<IReadOnlyList<CvTemplateDto>> TemplatesAsync(bool admin, CancellationToken ct);
    Task<CvTemplateDto?> TemplateAsync(Guid id, bool admin, CancellationToken ct);
    Task<CvTemplateDto?> WriteTemplateAsync(
        Guid? id,
        Guid accountId,
        CvTemplateWrite input,
        CancellationToken ct
    );
    Task<IReadOnlyList<OnlineCvListDto>> ListAsync(Guid accountId, CancellationToken ct);
    Task<OnlineCvDto?> CreateAsync(Guid accountId, Guid templateId, CancellationToken ct);
    Task<OnlineCvDto?> GetAsync(Guid accountId, Guid id, CancellationToken ct);
    Task<OnlineCvDto?> SaveAsync(Guid accountId, Guid id, CvWrite input, CancellationToken ct);
    Task<bool> DeleteAsync(Guid accountId, Guid id, Guid version, CancellationToken ct);
}
