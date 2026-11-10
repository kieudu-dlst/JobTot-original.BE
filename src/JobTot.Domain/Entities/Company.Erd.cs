namespace JobTot.Domain.Entities;

public sealed partial class Company : AuditableEntity
{
    public Guid? IndustryId { get; set; }
    public Industry? Industry { get; set; }
    public Guid? HeadOfficeProvinceId { get; set; }
    public Province? HeadOfficeProvince { get; set; }
    public string? LegalName { get; set; }
    public string? TaxCode { get; set; }
    public string? Slug { get; set; }
    public Guid? LogoFileId { get; set; }
    public Media? LogoFile { get; set; }
    public Guid? CoverFileId { get; set; }
    public Media? CoverFile { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public int? FoundedYear { get; set; }
    public int? CompanySizeMin { get; set; }
    public int? CompanySizeMax { get; set; }
    public string? Culture { get; set; }
    public string? VerificationStatus { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string DisplayName
    {
        get => Name;
        set => Name = value;
    }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? About
    {
        get => Description;
        set => Description = value ?? string.Empty;
    }
    public ICollection<CompanyMedia> CompanyMedias { get; set; } = new List<CompanyMedia>();
    public ICollection<RecruiterProfile> RecruiterProfiles { get; set; } =
        new List<RecruiterProfile>();
    public ICollection<RecruitmentCampaign> RecruitmentCampaigns { get; set; } =
        new List<RecruitmentCampaign>();
    public ICollection<TalentTag> TalentTags { get; set; } = new List<TalentTag>();
    public ICollection<FollowedCompany> FollowedCompanys { get; set; } =
        new List<FollowedCompany>();
    public ICollection<TalentPoolEntry> TalentPoolEntrys { get; set; } =
        new List<TalentPoolEntry>();
    public ICollection<CompanyIndustry> CompanyIndustrys { get; set; } =
        new List<CompanyIndustry>();
}
