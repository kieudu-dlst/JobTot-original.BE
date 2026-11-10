using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

internal static class ErdModelConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        {
            var entity = modelBuilder.Entity<Account>();
            entity.ToTable("Account");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.PasswordHash).HasMaxLength(1024);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AccountType).HasMaxLength(200);
            entity.Property(x => x.AccountStatus).HasMaxLength(200);
        }
        {
            var entity = modelBuilder.Entity<ExternalLogin>();
            entity.ToTable("ExternalLogin");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Account)
                .WithMany(x => x.ExternalLogins)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Provider).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProviderSubject).HasMaxLength(256).IsRequired();
        }
        {
            var entity = modelBuilder.Entity<Province>();
            entity.ToTable("Province");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.ProvinceName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProvinceCode).HasMaxLength(200).IsRequired();
        }
        {
            var entity = modelBuilder.Entity<Industry>();
            entity.ToTable("Industry");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.IndustryName).HasMaxLength(200).IsRequired();
        }
        {
            var entity = modelBuilder.Entity<Company>();
            entity.ToTable("Companies");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Industry)
                .WithMany(x => x.Companys)
                .HasForeignKey(x => x.IndustryId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.HeadOfficeProvince)
                .WithMany(x => x.Companys)
                .HasForeignKey(x => x.HeadOfficeProvinceId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.LegalName).HasMaxLength(200);
            entity.Property(x => x.TaxCode).HasMaxLength(200);
            entity.Property(x => x.Slug).HasMaxLength(200);
            entity
                .HasOne(x => x.LogoFile)
                .WithMany(x => x.CompanyLogoFiles)
                .HasForeignKey(x => x.LogoFileId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.CoverFile)
                .WithMany(x => x.CompanyCoverFiles)
                .HasForeignKey(x => x.CoverFileId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.WebsiteUrl).HasMaxLength(2048);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.AddressLine).HasMaxLength(200);
            entity.Property(x => x.Culture).HasColumnType("nvarchar(max)");
            entity.Property(x => x.VerificationStatus).HasMaxLength(200);
            entity.Property(x => x.Name).HasColumnName("DisplayName");
            entity.Property(x => x.Description).HasColumnName("About");
        }
        {
            var entity = modelBuilder.Entity<CompanyMedia>();
            entity.ToTable("CompanyMedia");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.CompanyMedias)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Media)
                .WithMany(x => x.CompanyMedias)
                .HasForeignKey(x => x.MediaId)
                .OnDelete(DeleteBehavior.NoAction);
        }
        {
            var entity = modelBuilder.Entity<RecruiterProfile>();
            entity.ToTable("RecruiterProfile");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Account)
                .WithMany(x => x.RecruiterProfiles)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.RecruiterProfiles)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.JobTitle).HasMaxLength(200);
            entity.Property(x => x.WorkPhone).HasMaxLength(32);
        }
        {
            var entity = modelBuilder.Entity<CandidateProfile>();
            entity.ToTable("CandidateProfile");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Account)
                .WithOne(x => x.CandidateProfile)
                .HasForeignKey<CandidateProfile>(x => x.AccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Province)
                .WithMany(x => x.CandidateProfiles)
                .HasForeignKey(x => x.ProvinceId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Headline).HasMaxLength(200);
            entity.Property(x => x.ExpectedSalaryMin).HasPrecision(18, 2);
            entity.Property(x => x.ExpectedSalaryMax).HasPrecision(18, 2);
            entity.Property(x => x.ReadyStatus).HasMaxLength(200);
            entity.Property(x => x.Summary).HasColumnType("nvarchar(max)");
        }
        {
            var entity = modelBuilder.Entity<Skill>();
            entity.ToTable("Skill");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.SkillName).HasMaxLength(200).IsRequired();
        }
        {
            var entity = modelBuilder.Entity<CandidateSkill>();
            entity.ToTable("CandidateSkill");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Candidate)
                .WithMany(x => x.CandidateSkills)
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Skill)
                .WithMany(x => x.CandidateSkills)
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.NoAction);
        }
        {
            var entity = modelBuilder.Entity<CvTemplate>();
            entity.ToTable("CvTemplate");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity
                .HasOne(x => x.Category)
                .WithMany(x => x.CvTemplates)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.PreviewUrl).HasMaxLength(2048);
        }
        {
            var entity = modelBuilder.Entity<CandidateCv>();
            entity.ToTable("CandidateCv");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Candidate)
                .WithMany(x => x.CandidateCvs)
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.CvTemplate)
                .WithMany(x => x.CandidateCvs)
                .HasForeignKey(x => x.CvTemplateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.FileUrl).HasMaxLength(2048);
            entity.Property(x => x.ContentJson).HasColumnType("nvarchar(max)");
        }
        {
            var entity = modelBuilder.Entity<EmploymentType>();
            entity.ToTable("EmploymentType");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.TypeName).HasMaxLength(200).IsRequired();
        }
        {
            var entity = modelBuilder.Entity<RecruitmentCampaign>();
            entity.ToTable("RecruitmentCampaign");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.RecruitmentCampaigns)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.OwnerRecruiter)
                .WithMany(x => x.RecruitmentCampaigns)
                .HasForeignKey(x => x.OwnerRecruiterId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Industry)
                .WithMany(x => x.RecruitmentCampaigns)
                .HasForeignKey(x => x.IndustryId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Province)
                .WithMany(x => x.RecruitmentCampaigns)
                .HasForeignKey(x => x.ProvinceId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.EmploymentType)
                .WithMany(x => x.RecruitmentCampaigns)
                .HasForeignKey(x => x.EmploymentTypeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Department).HasMaxLength(200);
            entity.Property(x => x.Description).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Requirements).HasColumnType("nvarchar(max)");
            entity.Property(x => x.WorkAddress).HasMaxLength(200);
            entity.Property(x => x.WorkMode).HasMaxLength(200);
            entity.Property(x => x.Seniority).HasMaxLength(200);
            entity.Property(x => x.EducationLevel).HasMaxLength(200);
            entity.Property(x => x.SalaryMin).HasPrecision(18, 2);
            entity.Property(x => x.SalaryMax).HasPrecision(18, 2);
            entity.Property(x => x.GenderPreference).HasMaxLength(200);
            entity.Property(x => x.CampaignStatus).HasMaxLength(200);
            entity.Property(x => x.Slug).HasMaxLength(200);
            entity.Property(x => x.PostingStatus).HasMaxLength(200);
        }
        {
            var entity = modelBuilder.Entity<CampaignSkill>();
            entity.ToTable("CampaignSkill");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Campaign)
                .WithMany(x => x.CampaignSkills)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Skill)
                .WithMany(x => x.CampaignSkills)
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.NoAction);
        }
        {
            var entity = modelBuilder.Entity<CampaignBenefit>();
            entity.ToTable("CampaignBenefit");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Campaign)
                .WithMany(x => x.CampaignBenefits)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Benefit)
                .WithMany(x => x.CampaignBenefits)
                .HasForeignKey(x => x.BenefitId)
                .OnDelete(DeleteBehavior.NoAction);
        }
        {
            var entity = modelBuilder.Entity<CampaignStage>();
            entity.ToTable("CampaignStage");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Campaign)
                .WithMany(x => x.CampaignStages)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.StageName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.StageKind).HasMaxLength(200);
        }
        {
            var entity = modelBuilder.Entity<JobApplication>();
            entity.ToTable("Application");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Campaign)
                .WithMany(x => x.JobApplications)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Candidate)
                .WithMany(x => x.JobApplications)
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Cv)
                .WithMany(x => x.JobApplications)
                .HasForeignKey(x => x.CvId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Stage)
                .WithMany(x => x.JobApplications)
                .HasForeignKey(x => x.StageId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Source).HasMaxLength(200);
            entity.Property(x => x.ApplicationStatus).HasMaxLength(200);
            entity.Property(x => x.CoverLetter).HasColumnType("nvarchar(max)");
            entity.Property(x => x.MatchScore).HasPrecision(18, 2);
        }
        {
            var entity = modelBuilder.Entity<ApplicationStageHistory>();
            entity.ToTable("ApplicationStageHistory");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Application)
                .WithMany(x => x.ApplicationStageHistorys)
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Stage)
                .WithMany(x => x.ApplicationStageHistorys)
                .HasForeignKey(x => x.StageId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.ChangedByRecruiter)
                .WithMany(x => x.ApplicationStageHistorys)
                .HasForeignKey(x => x.ChangedByRecruiterId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Note).HasColumnType("nvarchar(max)");
        }
        {
            var entity = modelBuilder.Entity<Interview>();
            entity.ToTable("Interview");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Application)
                .WithMany(x => x.Interviews)
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Stage)
                .WithMany(x => x.Interviews)
                .HasForeignKey(x => x.StageId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.MeetingUrl).HasMaxLength(2048);
            entity.Property(x => x.Location).HasMaxLength(200);
            entity.Property(x => x.InterviewStatus).HasMaxLength(200);
        }
        {
            var entity = modelBuilder.Entity<InterviewFeedback>();
            entity.ToTable("InterviewFeedback");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Interview)
                .WithMany(x => x.InterviewFeedbacks)
                .HasForeignKey(x => x.InterviewId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Recruiter)
                .WithMany(x => x.InterviewFeedbacks)
                .HasForeignKey(x => x.RecruiterId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Score).HasPrecision(18, 2);
            entity.Property(x => x.Decision).HasMaxLength(200);
            entity.Property(x => x.Comment).HasColumnType("nvarchar(max)");
        }
        {
            var entity = modelBuilder.Entity<TalentTag>();
            entity.ToTable("TalentTag");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.TalentTags)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.TagName).HasMaxLength(200).IsRequired();
        }
        {
            var entity = modelBuilder.Entity<TalentPoolTag>();
            entity.ToTable("TalentPoolTag");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.TalentPoolEntry)
                .WithMany(x => x.TalentPoolTags)
                .HasForeignKey(x => x.TalentPoolEntryId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.TalentTag)
                .WithMany(x => x.TalentPoolTags)
                .HasForeignKey(x => x.TalentTagId)
                .OnDelete(DeleteBehavior.NoAction);
        }
        {
            var entity = modelBuilder.Entity<SavedJob>();
            entity.ToTable("SavedJob");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Candidate)
                .WithMany(x => x.SavedJobs)
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.RecruitmentCampaign)
                .WithMany(x => x.SavedJobs)
                .HasForeignKey(x => x.RecruitmentCampaignId)
                .OnDelete(DeleteBehavior.NoAction);
        }
        {
            var entity = modelBuilder.Entity<FollowedCompany>();
            entity.ToTable("FollowedCompany");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Candidate)
                .WithMany(x => x.FollowedCompanys)
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.FollowedCompanys)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
        }
        {
            var entity = modelBuilder.Entity<ArticleCategory>();
            entity.ToTable("ArticleCategory");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.CategoryName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(200);
        }
        {
            var entity = modelBuilder.Entity<Article>();
            entity.ToTable("Article");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.ArticleCategory)
                .WithMany(x => x.Articles)
                .HasForeignKey(x => x.ArticleCategoryId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.AuthorAccount)
                .WithMany(x => x.Articles)
                .HasForeignKey(x => x.AuthorAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(200);
            entity.Property(x => x.Summary).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Body).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CoverUrl).HasMaxLength(2048);
        }
        {
            var entity = modelBuilder.Entity<Media>();
            entity.ToTable("Media");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(200);
        }
        {
            var entity = modelBuilder.Entity<TalentPoolEntry>();
            entity.ToTable("TalentPoolEntry");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.TalentPoolEntrys)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Candidate)
                .WithMany(x => x.TalentPoolEntrys)
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.AddedByRecruiter)
                .WithMany(x => x.TalentPoolEntrys)
                .HasForeignKey(x => x.AddedByRecruiterId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.ReadinessStatus).HasMaxLength(200);
            entity.Property(x => x.Notes).HasColumnType("nvarchar(max)");
        }
        {
            var entity = modelBuilder.Entity<CompanyIndustry>();
            entity.ToTable("CompanyIndustry");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.CompanyIndustrys)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.Industry)
                .WithMany(x => x.CompanyIndustrys)
                .HasForeignKey(x => x.IndustryId)
                .OnDelete(DeleteBehavior.NoAction);
        }
        {
            var entity = modelBuilder.Entity<Benefit>();
            entity.ToTable("Benefit");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.BenefitName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Detail).HasColumnType("nvarchar(max)");
        }
        {
            var entity = modelBuilder.Entity<CvCategory>();
            entity.ToTable("CvCategory");
            entity.HasKey(x => x.Id);
            entity
                .Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(EntityStatus.Active);
            entity.Property(x => x.CreatedDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity
                .HasOne(x => x.CreatedAccount)
                .WithMany()
                .HasForeignKey(x => x.CreatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(x => x.UpdatedAccount)
                .WithMany()
                .HasForeignKey(x => x.UpdatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        }
        modelBuilder
            .Entity<ExternalLogin>()
            .HasIndex(x => new { x.Provider, x.ProviderSubject })
            .IsUnique();
        modelBuilder
            .Entity<CandidateSkill>()
            .HasIndex(x => new { x.CandidateId, x.SkillId })
            .IsUnique();
        modelBuilder
            .Entity<CampaignSkill>()
            .HasIndex(x => new { x.CampaignId, x.SkillId })
            .IsUnique();
        modelBuilder
            .Entity<CampaignBenefit>()
            .HasIndex(x => new { x.CampaignId, x.BenefitId })
            .IsUnique();
        modelBuilder
            .Entity<CompanyMedia>()
            .HasIndex(x => new { x.CompanyId, x.MediaId })
            .IsUnique();
        modelBuilder
            .Entity<CompanyIndustry>()
            .HasIndex(x => new { x.CompanyId, x.IndustryId })
            .IsUnique();
        modelBuilder
            .Entity<TalentPoolTag>()
            .HasIndex(x => new { x.TalentPoolEntryId, x.TalentTagId })
            .IsUnique();
        modelBuilder
            .Entity<SavedJob>()
            .HasIndex(x => new { x.CandidateId, x.RecruitmentCampaignId })
            .IsUnique();
        modelBuilder
            .Entity<FollowedCompany>()
            .HasIndex(x => new { x.CandidateId, x.CompanyId })
            .IsUnique();
        modelBuilder.Entity<Account>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Account>().HasIndex(x => x.Phone).IsUnique();
        modelBuilder
            .Entity<CandidateCv>()
            .HasIndex(x => x.CandidateId)
            .IsUnique()
            .HasFilter("[IsDefault] = 1");
    }
}
