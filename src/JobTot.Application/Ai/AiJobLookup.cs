using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;

namespace JobTot.Application.Ai;

// Deliberate allow-list: never serialize Company, recruiter or application entities to AI.
public sealed record AiPublicJob(Guid Id, string Title, string CompanyName, string Industry,
    string Location, decimal? SalaryMin, decimal? SalaryMax, string SalaryPeriod,
    string CompanyDescription = "", int? FoundedYear = null)
{
    public string Url => $"/jobs/{Id:D}";
}
public sealed record AiJobMatches(bool Relevant, IReadOnlyList<AiPublicJob> Jobs);
public sealed class AiJobLookup(IRecruitmentData db, ICurrentActor actor)
{
    public static string Normalize(string value) => string.Concat(value.ToLowerInvariant().Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).Replace('đ', 'd');
    public static bool RequiresLookup(string question)
    {
        var text = Normalize(question);
        return new[] { "cong ty", "doanh nghiep", "nha tuyen dung", "tim viec", "viec lam", "tin tuyen",
            "dang tuyen", "tuyen dung", "tuyen nhan", "tuyen ke", "lam o dau", "lam viec o dau", "noi nao tuyen" }
            .Any(term => Regex.IsMatch(text, @"\b" + Regex.Escape(term) + @"\b"));
    }
    public static bool HasCareerTopic(string question) => Regex.IsMatch(Normalize(question), @"\b(nganh|nghe|it|cntt|developer|security|ke toan)\b");
    public static string[] RoleTerms(string question)
    {
        var text = Normalize(question);
        if (Regex.IsMatch(text, @"\b(ke toan|accountant|accounting|kiem toan|auditor)\b"))
            return ["kế toán", "ke toan", "accountant", "accounting", "kiểm toán", "auditor"];
        if (Regex.IsMatch(text, @"\b(an ninh mang|bao mat|cybersecurity|security)\b"))
            return ["an ninh", "bảo mật", "security", "cyber", "soc analyst", "pentest"];
        if (Regex.IsMatch(text, @"\b(it|cntt|cong nghe thong tin|phan mem|lap trinh)\b"))
            return [" it ", " cntt ", "developer", "lập trình", "phần mềm", "software", "devops", "tester", "kiểm thử", "security", "bảo mật", "data engineer", "data analyst", "kỹ sư dữ liệu", "quản trị mạng", "backend", "frontend", "fullstack", "full stack"];
        if (Regex.IsMatch(text, @"\b(backend|frontend|fullstack|developer)\b"))
            return ["developer", "lập trình", "backend", "frontend", "fullstack", "full stack"];
        return [];
    }
    public static bool RefersToEarlierTopic(string question) => new[] { "nganh nay", "nganh do", "nghe nay", "linh vuc nay", "cong ty do", "the con", "vay con" }
        .Any(Normalize(question).Contains);
    public static bool EntryLevel(string question) => new[] { "moi ra truong", "thuc tap", "junior", "fresher", "chua co kinh nghiem", "khong co kinh nghiem" }
        .Any(Normalize(question).Contains);
    public async Task<AiJobMatches> FindAsync(string question, CancellationToken ct, bool entryLevel = false, bool requested = false)
    {
        // Scope is fixed by authenticated actor. No prompt can elevate it to private/company data.
        if (actor.AccountId == Guid.Empty) throw new CareerSearchException("ai_forbidden", "Vui lòng đăng nhập.");
        if (!requested && !RequiresLookup(question)) return new(false, []);
        var normalized = Normalize(question);
        var industries = await db.IndustryRecords.ReadOnly().Where(i => i.Status == EntityStatus.Active)
            .Select(i => new { i.Id, i.IndustryName }).ToListAsync(ct);
        var industryTerms = industries.Where(i => normalized.Contains(Normalize(i.IndustryName)))
            .Select(i => i.IndustryName.ToLowerInvariant()).ToArray();
        var now = DateTimeOffset.UtcNow;
        var jobs = db.Jobs.ReadOnly().Where(j => !j.IsClosed && j.ExpiresAt > now && j.Company.Status == EntityStatus.Active);
        if (entryLevel || EntryLevel(question))
            jobs = jobs.Where(j => j.EmploymentType == "internship" || j.PositionLevel == "intern"
                || j.ExperienceLevel == "none" || j.ExperienceLevel == "under-1"
                || j.Title.ToLower().Contains("intern") || j.Title.ToLower().Contains("junior")
                || j.Title.ToLower().Contains("fresher") || j.Title.ToLower().Contains("thực tập"));
        // Mirrored campaign rows must also be published and active. Draft/private rows never enter context.
        jobs = jobs.Where(j => !db.RecruitmentCampaignRecords.Any(c => c.Id == j.Id)
            || db.RecruitmentCampaignRecords.Any(c => c.Id == j.Id && c.CompanyId == j.CompanyId
                && c.Status == EntityStatus.Active && c.CampaignStatus == "Running" && c.PublishedAt != null && c.PublishedAt <= now
                && (c.ExpiresAt == null || c.ExpiresAt > now)));
        var terms = RoleTerms(question);
        var matchCompany = false;
        if (terms.Length == 0) terms = industryTerms;
        if (terms.Length == 0)
        {
            // Match meaningful adjacent words; parameterized LINQ, never SQL supplied by the model.
            var words = Regex.Matches(question.ToLowerInvariant(), @"[\p{L}\p{N}+#.]+")
                .Select(m => m.Value).ToArray();
            var stop = new[] { "mình", "tôi", "có", "không", "nào", "cho", "bạn", "là", "của", "và", "đang", "muốn", "hãy", "giúp", "về", "công", "ty", "tìm", "việc", "tuyển", "jobtot" };
            terms = words.Zip(words.Skip(1), (a,b) => (a,b)).Where(t => !stop.Contains(t.a) && !stop.Contains(t.b))
                .Select(t => t.a + " " + t.b).Distinct().Take(12).ToArray();
            matchCompany = true;
        }
        if (terms.Length == 0) return new(true, []);
        var parameter = Expression.Parameter(typeof(JobPost), "job");
        Expression body = Expression.Constant(false);
        foreach (var term in terms)
        {
            Expression<Func<JobPost, bool>> match = j => (" " +
                (j.Title.EndsWith(" - " + j.Company.Name) || j.Title.EndsWith(" — " + j.Company.Name) || j.Title.EndsWith(" – " + j.Company.Name)
                    ? j.Title.Substring(0, j.Title.Length - j.Company.Name.Length - 3) : j.Title).ToLower() + " ").Contains(term)
                || (matchCompany && j.Company.Name.ToLower().Contains(term));
            body = Expression.OrElse(body, new ReplaceParameter(match.Parameters[0], parameter).Visit(match.Body)!);
        }
        jobs = jobs.Where(Expression.Lambda<Func<JobPost, bool>>(body, parameter));
        var rows = await jobs.OrderByDescending(j => j.CreatedAt).ThenBy(j => j.Id).Take(6)
            .Select(j => new AiPublicJob(j.Id, j.Title, j.Company.Name,
                j.Company.Industry == null ? "" : j.Company.Industry.IndustryName,
                j.Location,
                db.RecruitmentCampaignRecords.Any(c => c.Id == j.Id && !c.SalaryVisible) ? null : j.SalaryMin,
                db.RecruitmentCampaignRecords.Any(c => c.Id == j.Id && !c.SalaryVisible) ? null : j.SalaryMax,
                j.SalaryPeriod, j.Company.Description, j.Company.FoundedYear)).ToListAsync(ct);
        return new(true, rows);
    }
    private sealed class ReplaceParameter(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : node;
    }
}
