namespace JobTot.Domain.Entities;

public sealed class JobPost
{
    private JobPost() { }

    public JobPost(
        Guid companyId,
        string title,
        string description,
        string location,
        decimal? salaryMin,
        decimal? salaryMax,
        DateTimeOffset expiresAt
    )
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company is required.");
        CompanyId = companyId;
        Update(title, description, location, salaryMin, salaryMax, expiresAt);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public Guid? ProvinceId { get; private set; }
    public Province? Province { get; private set; }

    public void SetProvince(Guid? provinceId)
    {
        if (IsClosed)
            throw new InvalidOperationException("Closed jobs cannot be edited.");
        if (provinceId == Guid.Empty)
            throw new ArgumentException("Invalid province.");
        ProvinceId = provinceId;
    }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public decimal? SalaryMin { get; private set; }
    public decimal? SalaryMax { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsClosed { get; private set; }
    public string? ExperienceLevel { get; private set; }
    public string? PositionLevel { get; private set; }
    public string? EmploymentType { get; private set; }
    public string SalaryPeriod { get; private set; } = "monthly";
    public string? WorkSchedule { get; private set; }
    public string? SalaryNote { get; private set; }
    public string? Benefits { get; private set; }

    public string? Requirements { get; private set; }
    public string? HiringProcess { get; private set; }
    public int? Vacancies { get; private set; }
    public string? GenderRequirement { get; private set; }
    public string? AgeRequirement { get; private set; }
    public string? RecruiterName { get; private set; }
    public string? RecruiterTitle { get; private set; }
    public string? RecruiterEmail { get; private set; }

    public void SetCandidateDetails(
        string? requirements,
        string? hiringProcess,
        int? vacancies,
        string? genderRequirement,
        string? ageRequirement,
        string? recruiterName,
        string? recruiterTitle,
        string? recruiterEmail
    )
    {
        if (IsClosed)
            throw new InvalidOperationException("Closed jobs cannot be edited.");
        if (
            requirements?.Length > 10000
            || hiringProcess?.Length > 4000
            || vacancies is <= 0 or > 10000
            || genderRequirement?.Length > 100
            || ageRequirement?.Length > 100
            || recruiterName?.Length > 200
            || recruiterTitle?.Length > 200
            || recruiterEmail?.Length > 254
        )
            throw new ArgumentException("Invalid candidate job details.");
        Requirements = requirements;
        HiringProcess = hiringProcess;
        Vacancies = vacancies;
        GenderRequirement = genderRequirement;
        AgeRequirement = ageRequirement;
        RecruiterName = recruiterName;
        RecruiterTitle = recruiterTitle;
        RecruiterEmail = recruiterEmail;
    }

    public void SetDirectoryDetails(
        string? experienceLevel,
        string? positionLevel,
        string? employmentType,
        string salaryPeriod = "monthly",
        string? workSchedule = null,
        string? salaryNote = null,
        string? benefits = null
    )
    {
        if (IsClosed)
            throw new InvalidOperationException("Closed jobs cannot be edited.");
        if (
            experienceLevel is not null
            && !new[] { "none", "under-1", "1-3", "3-5", "over-5" }.Contains(experienceLevel)
        )
            throw new ArgumentException("Invalid experience level.");
        if (
            positionLevel is not null
            && !new[] { "intern", "employee", "supervisor", "manager", "director" }.Contains(
                positionLevel
            )
        )
            throw new ArgumentException("Invalid position level.");
        if (
            employmentType is not null
            && !new[] { "full-time", "part-time", "internship", "remote", "flexible" }.Contains(
                employmentType
            )
        )
            throw new ArgumentException("Invalid employment type.");
        if (!new[] { "monthly", "daily", "hourly" }.Contains(salaryPeriod))
            throw new ArgumentException("Invalid salary period.");
        if (workSchedule?.Length > 200 || salaryNote?.Length > 200 || benefits?.Length > 1000)
            throw new ArgumentException("Job directory details are too long.");
        ExperienceLevel = experienceLevel;
        PositionLevel = positionLevel;
        EmploymentType = employmentType;
        SalaryPeriod = salaryPeriod;
        WorkSchedule = workSchedule?.Trim();
        SalaryNote = salaryNote?.Trim();
        Benefits = benefits?.Trim();
    }

    public void Update(
        string title,
        string description,
        string location,
        decimal? salaryMin,
        decimal? salaryMax,
        DateTimeOffset expiresAt
    )
    {
        if (IsClosed)
            throw new InvalidOperationException("Closed jobs cannot be edited.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            throw new ArgumentException("Title must contain 1-200 characters.");
        if (string.IsNullOrWhiteSpace(description) || description.Length > 10000)
            throw new ArgumentException("Description must contain 1-10000 characters.");
        if (string.IsNullOrWhiteSpace(location) || location.Trim().Length > 200)
            throw new ArgumentException("Location must contain 1-200 characters.");
        if (
            salaryMin < 0
            || salaryMax < 0
            || salaryMin > salaryMax
            || salaryMin > 9999999999999999.99m
            || salaryMax > 9999999999999999.99m
        )
            throw new ArgumentException("Invalid salary range.");
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Expiry must be in the future.");
        Title = title.Trim();
        Description = description;
        Location = location.Trim();
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        ExpiresAt = expiresAt;
    }

    public void Close() => IsClosed = true;

    public void ResumeCampaign() => IsClosed = false;

    public JobPost(
        Guid campaignId,
        Guid companyId,
        string title,
        string description,
        string location,
        decimal? salaryMin,
        decimal? salaryMax,
        DateTimeOffset expiresAt
    )
        : this(companyId, title, description, location, salaryMin, salaryMax, expiresAt)
    {
        if (campaignId == Guid.Empty)
            throw new ArgumentException("Campaign ID is required.");
        Id = campaignId;
    }
}
