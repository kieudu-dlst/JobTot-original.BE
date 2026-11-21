using System.Data;

namespace JobTot.Application.Persistence;

public interface IRecruitmentData
{
    IEntityCollection<DemoDataset> DemoDatasets
    {
        get;
    }
    IEntityCollection<CareerArticle> CareerArticles
    {
        get;
    }
    IEntityCollection<ChatConversation> ChatConversations
    {
        get;
    }
    IEntityCollection<ChatMessage> ChatMessages
    {
        get;
    }
    IEntityCollection<AccountEmailChallenge> AccountEmailChallenges
    {
        get;
    }
    IEntityCollection<AccountNotificationRead> AccountNotificationReads
    {
        get;
    }
    IEntityCollection<AccountMessageRead> AccountMessageReads
    {
        get;
    }
    IEntityCollection<Company> Companies
    {
        get;
    }
    IEntityCollection<CandidateJobSubmission> CandidateJobSubmissions
    {
        get;
    }
    IEntityCollection<CandidateSavedJob> CandidateSavedJobs
    {
        get;
    }
    IEntityCollection<JobPost> Jobs
    {
        get;
    }
    IEntityCollection<Account> AccountRecords
    {
        get;
    }
    IEntityCollection<CandidateRefreshSession> CandidateRefreshSessions
    {
        get;
    }
    IEntityCollection<UsedCandidateRefreshToken> UsedCandidateRefreshTokens
    {
        get;
    }
    IEntityCollection<ExternalLogin> ExternalLoginRecords
    {
        get;
    }
    IEntityCollection<ExternalAuthFlow> ExternalAuthFlows
    {
        get;
    }
    IEntityCollection<Province> ProvinceRecords
    {
        get;
    }
    IEntityCollection<Industry> IndustryRecords
    {
        get;
    }
    IEntityCollection<CompanyMedia> CompanyMediaRecords
    {
        get;
    }
    IEntityCollection<RecruiterProfile> RecruiterProfileRecords
    {
        get;
    }
    IEntityCollection<CandidateProfile> CandidateProfileRecords
    {
        get;
    }
    IEntityCollection<Skill> SkillRecords
    {
        get;
    }
    IEntityCollection<CandidateSkill> CandidateSkillRecords
    {
        get;
    }
    IEntityCollection<CvTemplate> CvTemplateRecords
    {
        get;
    }
    IEntityCollection<CandidateCv> CandidateCvRecords
    {
        get;
    }
    IEntityCollection<EmploymentType> EmploymentTypeRecords
    {
        get;
    }
    IEntityCollection<RecruitmentCampaign> RecruitmentCampaignRecords
    {
        get;
    }
    IEntityCollection<CampaignSkill> CampaignSkillRecords
    {
        get;
    }
    IEntityCollection<CampaignBenefit> CampaignBenefitRecords
    {
        get;
    }
    IEntityCollection<CampaignStage> CampaignStageRecords
    {
        get;
    }
    IEntityCollection<JobApplication> JobApplicationRecords
    {
        get;
    }
    IEntityCollection<ApplicationStageHistory> ApplicationStageHistoryRecords
    {
        get;
    }
    IEntityCollection<Interview> InterviewRecords
    {
        get;
    }
    IEntityCollection<InterviewFeedback> InterviewFeedbackRecords
    {
        get;
    }
    IEntityCollection<TalentTag> TalentTagRecords
    {
        get;
    }
    IEntityCollection<TalentPoolTag> TalentPoolTagRecords
    {
        get;
    }
    IEntityCollection<SavedJob> SavedJobRecords
    {
        get;
    }
    IEntityCollection<FollowedCompany> FollowedCompanyRecords
    {
        get;
    }
    IEntityCollection<ArticleCategory> ArticleCategoryRecords
    {
        get;
    }
    IEntityCollection<Article> ArticleRecords
    {
        get;
    }
    IEntityCollection<Media> MediaRecords
    {
        get;
    }
    IEntityCollection<TalentPoolEntry> TalentPoolEntryRecords
    {
        get;
    }
    IEntityCollection<EmployerCandidateInterest> EmployerCandidateInterests
    {
        get;
    }
    IEntityCollection<CompanyIndustry> CompanyIndustryRecords
    {
        get;
    }
    IEntityCollection<Benefit> BenefitRecords
    {
        get;
    }
    IEntityCollection<CvCategory> CvCategoryRecords
    {
        get;
    }

    IEntityCollection<T> Set<T>() where T : class;
    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    void Reset();
    void Detach<T>(T entity) where T : class;
    Task ReloadAsync<T>(T entity, CancellationToken ct) where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    bool SupportsTransactions
    {
        get;
    }
    Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct);
    Task<IDataTransaction> BeginTransactionAsync(IsolationLevel isolation, CancellationToken ct);
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
}
public interface IDataTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
public interface IEntityCollection<T> : IQueryable<T> where T : class
{
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}
public class DataWriteException(string message, Exception inner, bool duplicate = false) : Exception(message, inner)
{
    public bool IsDuplicate { get; } = duplicate;
}
public sealed class DataConcurrencyException(string message, Exception inner) : DataWriteException(message, inner);
public sealed class DataOperationCanceledException(Exception inner) : OperationCanceledException("Data operation canceled.", inner);

public interface IDataFailureClassifier
{
    bool IsConcurrency(Exception exception);
}
