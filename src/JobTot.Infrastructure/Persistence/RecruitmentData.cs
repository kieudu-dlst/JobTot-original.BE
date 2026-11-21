using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using JobTot.Application.Persistence;
using JobTot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;

namespace JobTot.Infrastructure.Persistence;

public sealed class RecruitmentData(RecruitmentDbContext db) : IRecruitmentData
{
    public IEntityCollection<DemoDataset> DemoDatasets => Set<DemoDataset>();
    public IEntityCollection<CareerArticle> CareerArticles => Set<CareerArticle>();
    public IEntityCollection<ChatConversation> ChatConversations => Set<ChatConversation>();
    public IEntityCollection<ChatMessage> ChatMessages => Set<ChatMessage>();
    public IEntityCollection<AccountEmailChallenge> AccountEmailChallenges => Set<AccountEmailChallenge>();
    public IEntityCollection<AccountNotificationRead> AccountNotificationReads => Set<AccountNotificationRead>();
    public IEntityCollection<AccountMessageRead> AccountMessageReads => Set<AccountMessageRead>();
    public IEntityCollection<Company> Companies => Set<Company>();
    public IEntityCollection<CandidateJobSubmission> CandidateJobSubmissions => Set<CandidateJobSubmission>();
    public IEntityCollection<CandidateSavedJob> CandidateSavedJobs => Set<CandidateSavedJob>();
    public IEntityCollection<JobPost> Jobs => Set<JobPost>();
    public IEntityCollection<Account> AccountRecords => Set<Account>();
    public IEntityCollection<CandidateRefreshSession> CandidateRefreshSessions => Set<CandidateRefreshSession>();
    public IEntityCollection<UsedCandidateRefreshToken> UsedCandidateRefreshTokens => Set<UsedCandidateRefreshToken>();
    public IEntityCollection<ExternalLogin> ExternalLoginRecords => Set<ExternalLogin>();
    public IEntityCollection<ExternalAuthFlow> ExternalAuthFlows => Set<ExternalAuthFlow>();
    public IEntityCollection<Province> ProvinceRecords => Set<Province>();
    public IEntityCollection<Industry> IndustryRecords => Set<Industry>();
    public IEntityCollection<CompanyMedia> CompanyMediaRecords => Set<CompanyMedia>();
    public IEntityCollection<RecruiterProfile> RecruiterProfileRecords => Set<RecruiterProfile>();
    public IEntityCollection<CandidateProfile> CandidateProfileRecords => Set<CandidateProfile>();
    public IEntityCollection<Skill> SkillRecords => Set<Skill>();
    public IEntityCollection<CandidateSkill> CandidateSkillRecords => Set<CandidateSkill>();
    public IEntityCollection<CvTemplate> CvTemplateRecords => Set<CvTemplate>();
    public IEntityCollection<CandidateCv> CandidateCvRecords => Set<CandidateCv>();
    public IEntityCollection<EmploymentType> EmploymentTypeRecords => Set<EmploymentType>();
    public IEntityCollection<RecruitmentCampaign> RecruitmentCampaignRecords => Set<RecruitmentCampaign>();
    public IEntityCollection<CampaignSkill> CampaignSkillRecords => Set<CampaignSkill>();
    public IEntityCollection<CampaignBenefit> CampaignBenefitRecords => Set<CampaignBenefit>();
    public IEntityCollection<CampaignStage> CampaignStageRecords => Set<CampaignStage>();
    public IEntityCollection<JobApplication> JobApplicationRecords => Set<JobApplication>();
    public IEntityCollection<ApplicationStageHistory> ApplicationStageHistoryRecords => Set<ApplicationStageHistory>();
    public IEntityCollection<Interview> InterviewRecords => Set<Interview>();
    public IEntityCollection<InterviewFeedback> InterviewFeedbackRecords => Set<InterviewFeedback>();
    public IEntityCollection<TalentTag> TalentTagRecords => Set<TalentTag>();
    public IEntityCollection<TalentPoolTag> TalentPoolTagRecords => Set<TalentPoolTag>();
    public IEntityCollection<SavedJob> SavedJobRecords => Set<SavedJob>();
    public IEntityCollection<FollowedCompany> FollowedCompanyRecords => Set<FollowedCompany>();
    public IEntityCollection<ArticleCategory> ArticleCategoryRecords => Set<ArticleCategory>();
    public IEntityCollection<Article> ArticleRecords => Set<Article>();
    public IEntityCollection<Media> MediaRecords => Set<Media>();
    public IEntityCollection<TalentPoolEntry> TalentPoolEntryRecords => Set<TalentPoolEntry>();
    public IEntityCollection<EmployerCandidateInterest> EmployerCandidateInterests => Set<EmployerCandidateInterest>();
    public IEntityCollection<CompanyIndustry> CompanyIndustryRecords => Set<CompanyIndustry>();
    public IEntityCollection<Benefit> BenefitRecords => Set<Benefit>();
    public IEntityCollection<CvCategory> CvCategoryRecords => Set<CvCategory>();

    public IEntityCollection<T> Set<T>() where T : class => new EntityCollection<T>(db.Set<T>(), new DataQueryProvider(db));
    public void Add<T>(T entity) where T : class => db.Add(entity);
    public void Remove<T>(T entity) where T : class => db.Remove(entity);
    public void Reset() => db.ChangeTracker.Clear();
    public void Detach<T>(T entity) where T : class => db.Entry(entity).State = EntityState.Detached;
    public Task ReloadAsync<T>(T entity, CancellationToken ct) where T : class => db.Entry(entity).ReloadAsync(ct);
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException e) { throw new DataConcurrencyException(e.Message, e); }
        catch (DbUpdateException e) { throw new DataWriteException(e.Message, e, e.InnerException is SqlException { Number: 2601 or 2627 }); }
    }
    public bool SupportsTransactions => db.Database.IsRelational();
    public Task<IDataTransaction> BeginTransactionAsync(CancellationToken ct) => BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
    public async Task<IDataTransaction> BeginTransactionAsync(IsolationLevel isolation, CancellationToken ct) => new Transaction(await db.Database.BeginTransactionAsync(isolation, ct));
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                try
                {
                    return await operation();
                }
                catch (DataWriteException e) when (e.InnerException is DbUpdateException cause)
                {
                    // The EF execution strategy must see the original provider exception to decide
                    // whether to retry the entire transaction. Translate only after retries finish.
                    ExceptionDispatchInfo.Capture(cause).Throw();
                    throw;
                }
            });
        }
        catch (DbUpdateConcurrencyException e) { throw new DataConcurrencyException(e.Message, e); }
        catch (DbUpdateException e) { throw new DataWriteException(e.Message, e, e.InnerException is SqlException { Number: 2601 or 2627 }); }
    }
    private sealed class Transaction(IDbContextTransaction transaction) : IDataTransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
    private sealed class EntityCollection<T>(DbSet<T> set, DataQueryProvider provider) : IEntityCollection<T> where T : class
    {
        public Type ElementType => typeof(T);
        public Expression Expression => ((IQueryable<T>)set).Expression;
        public IQueryProvider Provider => provider;
        public IEnumerator<T> GetEnumerator() => provider.Read<T>(Expression).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(T entity) => set.Add(entity);
        public void AddRange(IEnumerable<T> entities) => set.AddRange(entities);
        public void Remove(T entity) => set.Remove(entity);
        public void RemoveRange(IEnumerable<T> entities) => set.RemoveRange(entities);
    }
}
