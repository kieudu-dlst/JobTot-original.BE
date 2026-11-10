using System.ComponentModel.DataAnnotations;
using JobTot.Application.Companies;

namespace JobTot.Application.Profiles;

public sealed class CandidateJobsQuery
{
    [RegularExpression("^(saved|applied|companies|interviews|hired)$")]
    public string Tab { get; set; } = "saved";

    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 3;

    [RegularExpression("^20[0-9]{2}-(0[1-9]|1[0-2])$")]
    public string? CalendarMonth { get; set; }
    public DateOnly? InterviewDate { get; set; }

    [Range(-840, 840)]
    public int TimeZoneOffsetMinutes { get; set; }
}

public record CandidateJobsStats(
    int Applied,
    int Viewed,
    int Interviews,
    int Saved,
    int FollowedCompanies
)
{
    public int Hired { get; init; }
    public int InterestedEmployers { get; init; }
}

public record CandidateEmployerInterestDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string? LogoUrl,
    string? Location,
    int? CompanySizeMin,
    int? CompanySizeMax,
    int OpenJobCount,
    DateTimeOffset InterestedAt,
    JobDto? Position = null
)
{
    public string? RecruiterName { get; init; }
    public string? RecruiterTitle { get; init; }
    public string? RecruiterEmail { get; init; }
}

public record CandidateInterviewDto(
    Guid Id,
    string JobTitle,
    string CompanyName,
    DateTimeOffset Start,
    DateTimeOffset End,
    string? Location,
    string? MeetingUrl,
    string? Status
)
{
    public Guid CompanyId { get; init; }
    public string? CompanyLogoUrl { get; init; }
    public bool CompanyVerified { get; init; }
    public string? StageName { get; init; }
    public int StageOrder { get; init; }
    public string? RecruiterName { get; init; }
    public string? RecruiterTitle { get; init; }
    public string? RecruiterEmail { get; init; }
    public string? Preparation { get; init; }
}

public record CandidateInterviewCalendarEvent(
    Guid Id,
    string JobTitle,
    DateTimeOffset Start,
    bool Online
);

public record CandidateJobApplicationItem(
    Guid JobId,
    string Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset? ReviewedAt
)
{
    public string? DetailUrl { get; init; }
    public Guid Id { get; init; }
    public string? CvTitle { get; init; }
    public string? RecruiterName { get; init; }
    public string? RecruiterEmail { get; init; }
    public CandidateApplicationProgress Progress { get; init; } = new(1, "Mới ứng tuyển");
}

public record CandidateJobsDashboard(
    string FullName,
    string? Headline,
    string? CvTitle,
    bool OpenToWork,
    CandidateJobsStats Stats,
    string Tab,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<JobDto> Jobs,
    IReadOnlyList<CandidateJobApplicationItem> Applications,
    IReadOnlyList<CompanyListingDto> Companies,
    IReadOnlyList<CandidateInterviewDto> Interviews,
    IReadOnlyList<JobDto> Deadlines,
    IReadOnlyList<JobDto> Recommendations
)
{
    public string ReadyStatus { get; init; } = "looking";
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<CandidateEmployerInterestDto> EmployerInterests { get; init; } = [];
    public IReadOnlyList<CandidateInterviewCalendarEvent> InterviewCalendar { get; init; } = [];
    public CandidateInterviewDto? NextInterview { get; init; }
}

public record CandidateReadyStatusRequest(
    [Required, RegularExpression("^(looking|considering|paused)$")] string ReadyStatus
);

public record CandidateAvailabilityRequest(bool OpenToWork);

public interface ICandidateJobsRepository
{
    Task<IReadOnlyList<Guid>> SavedIdsAsync(Guid accountId, CancellationToken ct);
    Task SetSavedAsync(Guid accountId, Guid jobId, bool saved, CancellationToken ct);
    Task<CandidateJobsDashboard> DashboardAsync(
        Guid accountId,
        CandidateJobsQuery query,
        CancellationToken ct
    );
    Task SetReadyStatusAsync(Guid accountId, string readyStatus, CancellationToken ct);
    Task SetAvailabilityAsync(Guid accountId, bool openToWork, CancellationToken ct);
    Task ConfirmInterviewAsync(Guid accountId, Guid interviewId, CancellationToken ct);
}
