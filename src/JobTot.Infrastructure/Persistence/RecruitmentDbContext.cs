using JobTot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

public sealed class RecruitmentDbContext(DbContextOptions<RecruitmentDbContext> options)
    : DbContext(options)
{
    public DbSet<DemoDataset> DemoDatasets => Set<DemoDataset>();
    public DbSet<CareerArticle> CareerArticles => Set<CareerArticle>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AccountEmailChallenge> AccountEmailChallenges => Set<AccountEmailChallenge>();
    public DbSet<AccountNotificationRead> AccountNotificationReads =>
        Set<AccountNotificationRead>();
    public DbSet<AccountMessageRead> AccountMessageReads => Set<AccountMessageRead>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CandidateJobSubmission> CandidateJobSubmissions => Set<CandidateJobSubmission>();
    public DbSet<CandidateSavedJob> CandidateSavedJobs => Set<CandidateSavedJob>();
    public DbSet<JobPost> Jobs => Set<JobPost>();
    public DbSet<Account> AccountRecords => Set<Account>();
    public DbSet<CandidateRefreshSession> CandidateRefreshSessions =>
        Set<CandidateRefreshSession>();
    public DbSet<UsedCandidateRefreshToken> UsedCandidateRefreshTokens =>
        Set<UsedCandidateRefreshToken>();
    public DbSet<ExternalLogin> ExternalLoginRecords => Set<ExternalLogin>();
    public DbSet<Province> ProvinceRecords => Set<Province>();
    public DbSet<Industry> IndustryRecords => Set<Industry>();
    public DbSet<CompanyMedia> CompanyMediaRecords => Set<CompanyMedia>();
    public DbSet<RecruiterProfile> RecruiterProfileRecords => Set<RecruiterProfile>();
    public DbSet<CandidateProfile> CandidateProfileRecords => Set<CandidateProfile>();
    public DbSet<Skill> SkillRecords => Set<Skill>();
    public DbSet<CandidateSkill> CandidateSkillRecords => Set<CandidateSkill>();
    public DbSet<CvTemplate> CvTemplateRecords => Set<CvTemplate>();
    public DbSet<CandidateCv> CandidateCvRecords => Set<CandidateCv>();
    public DbSet<EmploymentType> EmploymentTypeRecords => Set<EmploymentType>();
    public DbSet<RecruitmentCampaign> RecruitmentCampaignRecords => Set<RecruitmentCampaign>();
    public DbSet<CampaignSkill> CampaignSkillRecords => Set<CampaignSkill>();
    public DbSet<CampaignBenefit> CampaignBenefitRecords => Set<CampaignBenefit>();
    public DbSet<CampaignStage> CampaignStageRecords => Set<CampaignStage>();
    public DbSet<JobApplication> JobApplicationRecords => Set<JobApplication>();
    public DbSet<ApplicationStageHistory> ApplicationStageHistoryRecords =>
        Set<ApplicationStageHistory>();
    public DbSet<Interview> InterviewRecords => Set<Interview>();
    public DbSet<InterviewFeedback> InterviewFeedbackRecords => Set<InterviewFeedback>();
    public DbSet<TalentTag> TalentTagRecords => Set<TalentTag>();
    public DbSet<TalentPoolTag> TalentPoolTagRecords => Set<TalentPoolTag>();
    public DbSet<SavedJob> SavedJobRecords => Set<SavedJob>();
    public DbSet<FollowedCompany> FollowedCompanyRecords => Set<FollowedCompany>();
    public DbSet<ArticleCategory> ArticleCategoryRecords => Set<ArticleCategory>();
    public DbSet<Article> ArticleRecords => Set<Article>();
    public DbSet<Media> MediaRecords => Set<Media>();
    public DbSet<TalentPoolEntry> TalentPoolEntryRecords => Set<TalentPoolEntry>();
    public DbSet<EmployerCandidateInterest> EmployerCandidateInterests =>
        Set<EmployerCandidateInterest>();
    public DbSet<CompanyIndustry> CompanyIndustryRecords => Set<CompanyIndustry>();
    public DbSet<Benefit> BenefitRecords => Set<Benefit>();
    public DbSet<CvCategory> CvCategoryRecords => Set<CvCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEmailChallenge>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Role).HasMaxLength(20);
            entity.Property(x => x.Purpose).HasMaxLength(30);
            entity.Property(x => x.SecretHash).HasMaxLength(500);
            entity.Property(x => x.PasswordStamp).HasMaxLength(64);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasIndex(x => new
            {
                x.Email,
                x.Role,
                x.Purpose,
                x.CreatedAt,
            });
            entity
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AccountNotificationRead>(entity =>
        {
            entity.HasKey(x => new { x.AccountId, x.NotificationId });
            entity.Property(x => x.NotificationId).HasMaxLength(100);
            entity
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AccountMessageRead>(entity =>
        {
            entity.HasKey(x => new { x.AccountId, x.MessageId });
            entity
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne<ChatMessage>()
                .WithMany()
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<EmployerCandidateInterest>(entity =>
        {
            entity.ToTable("EmployerCandidateInterests");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.CandidateId }).IsUnique();
            entity
                .HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(x => x.Candidate)
                .WithMany()
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(x => x.InterestedByRecruiter)
                .WithMany()
                .HasForeignKey(x => x.InterestedByRecruiterId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CareerArticle>(entity =>
        {
            entity.Property(x => x.CoverUrl).HasMaxLength(500);
            entity.Property(x => x.CoverFileName).HasMaxLength(255);
            entity.Property(x => x.CoverContentType).HasMaxLength(100);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(600).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(20000).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.Published, x.UpdatedAt });
        });
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PairKey).HasMaxLength(73).IsRequired();
            entity.HasIndex(x => x.PairKey).IsUnique();
            entity.HasIndex(x => x.FirstAccountId);
            entity.HasIndex(x => x.SecondAccountId);
            entity
                .HasOne(x => x.FirstAccount)
                .WithMany()
                .HasForeignKey(x => x.FirstAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(x => x.SecondAccount)
                .WithMany()
                .HasForeignKey(x => x.SecondAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.ConversationId, x.Id });
            entity.HasIndex(x => new { x.SenderAccountId, x.ClientMessageId }).IsUnique();
            entity
                .HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(x => x.SenderAccount)
                .WithMany()
                .HasForeignKey(x => x.SenderAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DemoDataset>(entity =>
        {
            entity.ToTable("DemoDatasets");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(100);
            entity.Property(x => x.Payload).IsRequired();
        });
        ErdModelConfiguration.Configure(modelBuilder);
        modelBuilder.Entity<CvTemplate>().Property(t => t.Version).IsConcurrencyToken();
        modelBuilder.Entity<CvTemplate>().Property(t => t.Description).HasMaxLength(1000);
        modelBuilder.Entity<CvTemplate>().Property(t => t.PublicationState).HasMaxLength(20);
        modelBuilder.Entity<CandidateCv>().Property(c => c.DocumentVersion).IsConcurrencyToken();
        var session = modelBuilder.Entity<CandidateRefreshSession>();
        session.HasKey(x => x.Id);
        session.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        session.Property(x => x.PasswordStamp).HasMaxLength(64).IsRequired();
        session.Property(x => x.Version).IsConcurrencyToken();
        session
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        session.HasIndex(x => x.AccountId);
        var usedToken = modelBuilder.Entity<UsedCandidateRefreshToken>();
        usedToken.HasKey(x => x.TokenHash);
        usedToken.Property(x => x.TokenHash).HasMaxLength(64);
        usedToken
            .HasOne<CandidateRefreshSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        var company = modelBuilder.Entity<Company>();
        company.HasKey(x => x.Id);
        company.Property(x => x.Name).HasMaxLength(200).IsRequired();
        company.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        var submission = modelBuilder.Entity<CandidateJobSubmission>();
        var savedJob = modelBuilder.Entity<CandidateSavedJob>();
        savedJob.HasKey(x => x.Id);
        savedJob.HasIndex(x => new { x.CandidateId, x.JobId }).IsUnique();
        savedJob
            .HasOne(x => x.Candidate)
            .WithMany()
            .HasForeignKey(x => x.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);
        savedJob
            .HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Restrict);
        submission.HasKey(x => x.Id);
        submission.HasIndex(x => new { x.JobId, x.CandidateId }).IsUnique();
        submission
            .HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Restrict);
        submission
            .HasOne(x => x.Candidate)
            .WithMany()
            .HasForeignKey(x => x.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);
        submission
            .HasOne(x => x.Cv)
            .WithMany()
            .HasForeignKey(x => x.CvId)
            .OnDelete(DeleteBehavior.Restrict);
        submission.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        submission.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        submission.Property(x => x.CoverLetter).HasMaxLength(4000);
        submission.Property(x => x.CvTitle).HasMaxLength(200).IsRequired();
        submission.Property(x => x.Status).HasMaxLength(30).IsRequired();
        var job = modelBuilder.Entity<JobPost>();
        job.Property(x => x.Requirements).HasMaxLength(10000);
        job.Property(x => x.HiringProcess).HasMaxLength(4000);
        job.Property(x => x.GenderRequirement).HasMaxLength(100);
        job.Property(x => x.AgeRequirement).HasMaxLength(100);
        job.Property(x => x.RecruiterName).HasMaxLength(200);
        job.Property(x => x.RecruiterTitle).HasMaxLength(200);
        job.Property(x => x.RecruiterEmail).HasMaxLength(254);
        job.HasKey(x => x.Id);
        job.Property(x => x.Title).HasMaxLength(200).IsRequired();
        job.Property(x => x.Description).HasMaxLength(10000).IsRequired();
        job.Property(x => x.Location).HasMaxLength(200).IsRequired();
        job.Property(x => x.SalaryMin).HasPrecision(18, 2);
        job.Property(x => x.SalaryMax).HasPrecision(18, 2);
        job.Property<byte[]>("RowVersion").IsRowVersion();
        job.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        job.HasOne(x => x.Province)
            .WithMany()
            .HasForeignKey(x => x.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);
        job.HasIndex(x => new
        {
            x.ProvinceId,
            x.IsClosed,
            x.ExpiresAt,
        });
        job.Property(x => x.ExperienceLevel).HasMaxLength(20);
        job.Property(x => x.PositionLevel).HasMaxLength(20);
        job.Property(x => x.EmploymentType).HasMaxLength(20);
        job.Property(x => x.SalaryPeriod).HasMaxLength(10).HasDefaultValue("monthly");
        job.Property(x => x.WorkSchedule).HasMaxLength(200);
        job.Property(x => x.SalaryNote).HasMaxLength(200);
        job.Property(x => x.Benefits).HasMaxLength(1000);
        job.HasIndex(x => new { x.IsClosed, x.ExpiresAt });
        job.HasIndex(x => x.CreatedAt);
        job.ToTable(
            "Jobs",
            table =>
                table.HasCheckConstraint(
                    "CK_Jobs_Salary",
                    "([SalaryMin] IS NULL OR [SalaryMin] >= 0) AND ([SalaryMax] IS NULL OR [SalaryMax] >= 0) AND ([SalaryMin] IS NULL OR [SalaryMax] IS NULL OR [SalaryMin] <= [SalaryMax])"
                )
        );
        PerformanceModelConfiguration.Configure(modelBuilder);
    }
}
