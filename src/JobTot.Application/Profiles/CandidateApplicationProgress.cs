namespace JobTot.Application.Profiles;

public record CandidateApplicationProgress(
    int CurrentStep,
    string Label,
    bool Completed = false,
    bool Stopped = false
)
{
    public static CandidateApplicationProgress From(string status, DateTimeOffset? reviewedAt)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "invited" or "interview" or "interview_scheduled" or "interviewing" => new(
                2,
                "Đang phỏng vấn"
            ),
            "offer" or "offered" => new(3, "Đã gửi offer"),
            "hired" => new(4, "Đã nhận việc", Completed: true),
            "rejected" => new(5, "Không phù hợp", Stopped: true),
            "withdrawn" => new(1, "Đã rút hồ sơ", Stopped: true),
            "cancelled" or "canceled" => new(1, "Đã hủy ứng tuyển", Stopped: true),
            _ => new(1, "Mới ứng tuyển"),
        };
    }
}
