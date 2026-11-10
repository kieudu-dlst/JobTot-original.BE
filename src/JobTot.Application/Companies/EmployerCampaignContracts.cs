using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Companies;

public sealed class EmployerCampaignRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required, StringLength(200)]
    public string Department { get; set; } = "";

    [Required, StringLength(200)]
    public string WorkAddress { get; set; } = "";

    [StringLength(10000)]
    public string Description { get; set; } = "";

    [StringLength(10000)]
    public string Requirements { get; set; } = "";
    public Guid IndustryId { get; set; }
    public Guid ProvinceId { get; set; }
    public Guid EmploymentTypeId { get; set; }
    public Guid OwnerRecruiterId { get; set; }

    [RegularExpression("^(office|hybrid|remote)$")]
    public string WorkMode { get; set; } = "office";

    [RegularExpression("^(intern|employee|supervisor|manager|director)$")]
    public string Seniority { get; set; } = "employee";

    [Range(1, 10000)]
    public int Openings { get; set; } = 1;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [Range(0, 1000000000)]
    public decimal? SalaryMin { get; set; }

    [Range(0, 1000000000)]
    public decimal? SalaryMax { get; set; }
    public bool SalaryVisible { get; set; } = true;

    [Range(18, 100)]
    public int? AgeMin { get; set; }

    [Range(18, 100)]
    public int? AgeMax { get; set; }

    [RegularExpression("^(any|male|female)$")]
    public string GenderPreference { get; set; } = "any";

    [RegularExpression("^(Draft|Running|Paused|Ended)$")]
    public string CampaignStatus { get; set; } = "Draft";
    public DateTimeOffset? Revision { get; set; }
    public List<string> Skills { get; set; } = [];
    public List<EmployerCampaignBenefit> Benefits { get; set; } = [];
    public List<EmployerCampaignStage> Stages { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (SalaryMin > SalaryMax || AgeMin > AgeMax || StartDate > EndDate)
            yield return new("Khoảng lương, độ tuổi hoặc ngày bắt đầu/kết thúc không hợp lệ.");
        if (
            Skills is null
            || Skills.Count > 30
            || Skills.Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 200)
        )
            yield return new("Tối đa 30 kỹ năng hợp lệ.");
        if (
            Benefits is null
            || Benefits.Count > 20
            || Benefits.Any(b =>
                b is null
                || string.IsNullOrWhiteSpace(b.Title)
                || b.Title.Length > 200
                || b.Description is null
                || b.Description.Length > 2000
            )
        )
            yield return new("Phúc lợi không hợp lệ (tối đa 20 mục).");
        if (
            Stages is null
            || Stages.Count > 15
            || Stages.Any(s =>
                s is null
                || string.IsNullOrWhiteSpace(s.Name)
                || s.Name.Length > 200
                || s.Kind is null
                || s.Kind.Length > 200
            )
            || Stages.Where(s => s.Id.HasValue).GroupBy(s => s.Id).Any(g => g.Count() > 1)
        )
            yield return new("Quy trình tối đa 15 vòng; ID vòng phải duy nhất.");
        if (
            CampaignStatus == "Running"
            && (
                string.IsNullOrWhiteSpace(Description)
                || string.IsNullOrWhiteSpace(Requirements)
                || EndDate is null
                || StartDate is null
                || Stages?.Count == 0
            )
        )
            yield return new(
                "Để bắt đầu cần mô tả, yêu cầu, ngày bắt đầu/kết thúc và ít nhất một vòng tuyển dụng."
            );
    }
}

public sealed class EmployerCampaignBenefit
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class EmployerCampaignStage
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
}
