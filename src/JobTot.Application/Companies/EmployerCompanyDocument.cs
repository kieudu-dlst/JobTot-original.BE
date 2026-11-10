using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Companies;

public sealed class EmployerCompanyDocument
{
    [Required, StringLength(200)]
    public string Name { get; set; } = "";

    [StringLength(4000)]
    public string Description { get; set; } = "";

    [StringLength(200)]
    public string Address { get; set; } = "";

    [StringLength(2048)]
    public string Website { get; set; } = "";

    [Range(1800, 2100)]
    public int? FoundedYear { get; set; }

    [Range(1, 1000000)]
    public int? SizeMin { get; set; }

    [Range(1, 1000000)]
    public int? SizeMax { get; set; }
    public EmployerCompanyBrand Brand { get; set; } = new();

    [Required, StringLength(64)]
    public string Revision { get; set; } = "";
}

public sealed class EmployerCompanyBrand
{
    public int SchemaVersion { get; set; } = 1;
    public Guid UpdateStamp { get; set; }
    public string CoverUrl { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string EnglishName { get; set; } = "";
    public string Representative { get; set; } = "";
    public string Capital { get; set; } = "";
    public string WorkingHours { get; set; } = "";
    public string Industry { get; set; } = "";
    public List<Guid>? IndustryIds { get; set; }
    public string Story { get; set; } = "";
    public string Values { get; set; } = "";
    public string Workplace { get; set; } = "";
    public string WorkLife { get; set; } = "";
    public List<EmployerBrandItem> Highlights { get; set; } = [];
    public List<EmployerBrandItem> Benefits { get; set; } = [];
    public List<EmployerBrandItem> HiringProcess { get; set; } = [];
    public List<EmployerBrandItem> Activities { get; set; } = [];
    public List<string> Gallery { get; set; } = [];
}

public sealed class EmployerBrandItem
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tag { get; set; } = "";
}

public sealed record EmployerCompanyMember(
    Guid Id,
    string Name,
    string? Title,
    string? Email,
    bool IsCompanyAdmin
);

public sealed record EmployerCompanyResponse(
    Guid Id,
    string? LegalName,
    string? TaxCode,
    string? VerificationStatus,
    string? Province,
    bool CanEdit,
    int OpenJobs,
    int Followers,
    EmployerCompanyDocument Document,
    IReadOnlyList<EmployerCompanyMember> Team
);
