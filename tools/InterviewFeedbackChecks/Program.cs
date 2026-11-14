using System.Security.Claims;
using System.Text.Json;
using JobTot.Api.Controllers;
using JobTot.Domain.Entities;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<RecruitmentDbContext>()
    .UseInMemoryDatabase($"interview-feedback-{Guid.NewGuid()}")
    .Options;
var company = new Company("Feedback check company", "");
var recruiter = new RecruiterProfile
{
    Account = new Account { FullName = "Nhà tuyển dụng" },
    Company = company,
};
var colleague = new RecruiterProfile
{
    Account = new Account { FullName = "Đồng nghiệp" },
    Company = company,
};
var campaign = new RecruitmentCampaign { Company = company, Title = "Lập trình viên" };
var stage = new CampaignStage { Campaign = campaign, StageName = "Phỏng vấn" };
var interview = new Interview
{
    Application = new JobApplication
    {
        Campaign = campaign,
        Candidate = new CandidateProfile { Account = new Account { FullName = "Ứng viên" } },
    },
    Stage = stage,
    ScheduledStart = DateTimeOffset.UtcNow.AddHours(-2),
    ScheduledEnd = DateTimeOffset.UtcNow.AddHours(-1),
    InterviewStatus = "scheduled",
};
await using (var db = new RecruitmentDbContext(options))
{
    db.AddRange(recruiter, colleague, interview);
    await db.SaveChangesAsync();
}

EmployerCandidatesController Controller(RecruitmentDbContext db, Guid accountId) => new(db)
{
    ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, accountId.ToString())], "checks")),
        },
    },
};

void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine($"PASS {name}");
}

async Task Save(Guid accountId, decimal score, string decision, string comment)
{
    await using var db = new RecruitmentDbContext(options);
    var result = await Controller(db, accountId).Feedback(interview.Id, new()
    {
        Score = score,
        Decision = decision,
        Comment = comment,
    }, default);
    Check(result is OkResult, "Saving feedback succeeds");
}

async Task<JsonElement> Reload(Guid accountId)
{
    // A new context for every read prevents tracked entities from masking persistence bugs.
    await using var db = new RecruitmentDbContext(options);
    var day = DateOnly.FromDateTime(interview.ScheduledStart.ToOffset(TimeSpan.FromHours(7)).DateTime);
    var result = await Controller(db, accountId).Calendar(day, day, null, default);
    Check(result is OkObjectResult, "Reloading the interview calendar succeeds");
    return JsonSerializer.SerializeToElement(((OkObjectResult)result).Value,
        new JsonSerializerOptions(JsonSerializerDefaults.Web))[0];
}

const string comment = "Giao tiếp tốt.\nCó kinh nghiệm phù hợp với vị trí.";
await Save(recruiter.AccountId, 8.5m, "Pass", comment);
var reloaded = await Reload(recruiter.AccountId);
Check(reloaded.GetProperty("interviewStatus").GetString() == "completed", "Interview remains evaluated after reload");
var feedback = reloaded.GetProperty("feedbacks")[0];
Check(feedback.GetProperty("comment").GetString() == comment
    && feedback.GetProperty("score").GetDecimal() == 8.5m
    && feedback.GetProperty("decision").GetString() == "Pass", "Saved score, decision and multiline comment survive reload");
Check(feedback.GetProperty("isOwn").GetBoolean(), "Calendar identifies the current recruiter's evaluation");

await Save(colleague.AccountId, 6, "Consider", "Nhận xét riêng của đồng nghiệp");
await Save(recruiter.AccountId, 0, "Fail", "Cần thêm kinh nghiệm.");
reloaded = await Reload(recruiter.AccountId);
var feedbacks = reloaded.GetProperty("feedbacks").EnumerateArray().ToArray();
Check(feedbacks.Length == 2, "Updating feedback does not create a duplicate");
feedback = feedbacks.Single(f => f.GetProperty("isOwn").GetBoolean());
Check(feedback.GetProperty("score").GetDecimal() == 0
    && feedback.GetProperty("decision").GetString() == "Fail"
    && feedback.GetProperty("comment").GetString() == "Cần thêm kinh nghiệm.", "Editing replaces only the current recruiter's feedback");
Check(feedbacks.Single(f => !f.GetProperty("isOwn").GetBoolean()).GetProperty("comment").GetString()
    == "Nhận xét riêng của đồng nghiệp", "A colleague's evaluation is preserved");
reloaded = await Reload(colleague.AccountId);
Check(reloaded.GetProperty("feedbacks").EnumerateArray().Single(f => f.GetProperty("isOwn").GetBoolean())
    .GetProperty("comment").GetString() == "Nhận xét riêng của đồng nghiệp", "Ownership follows the signed-in recruiter after reload");
