using JobTot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

internal static class PerformanceModelConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        // Ordered history pages should read narrow indexes, not sort rows containing CV documents.
        modelBuilder
            .Entity<CandidateJobSubmission>()
            .HasIndex(x => new
            {
                x.CandidateId,
                x.AppliedAt,
                x.Id,
            })
            .IsDescending(false, true, false)
            .IncludeProperties(x => new
            {
                x.JobId,
                x.Status,
                x.ReviewedAt,
                x.CvTitle,
            });
        modelBuilder
            .Entity<CandidateSavedJob>()
            .HasIndex(x => new
            {
                x.CandidateId,
                x.SavedAt,
                x.JobId,
            })
            .IsDescending(false, true, false);

        modelBuilder
            .Entity<JobApplication>()
            .HasIndex(x => new
            {
                x.CandidateId,
                x.Status,
                x.AppliedAt,
                x.Id,
            })
            .IsDescending(false, false, true, false)
            .IncludeProperties(x => new { x.CampaignId, x.ApplicationStatus });
        modelBuilder
            .Entity<JobApplication>()
            .HasIndex(x => new
            {
                x.CampaignId,
                x.Status,
                x.AppliedAt,
                x.Id,
            })
            .IsDescending(false, false, true, false)
            .IncludeProperties(x => new
            {
                x.CandidateId,
                x.ApplicationStatus,
                x.Source,
            });

        modelBuilder
            .Entity<RecruitmentCampaign>()
            .HasIndex(x => new
            {
                x.CompanyId,
                x.Status,
                x.CreatedDate,
                x.Id,
            })
            .IsDescending(false, false, true, false)
            .IncludeProperties(x => x.CampaignStatus);
        modelBuilder
            .Entity<RecruiterProfile>()
            .HasIndex(x => new
            {
                x.AccountId,
                x.Status,
                x.JoinedAt,
                x.Id,
            })
            .IncludeProperties(x => x.CompanyId);
        modelBuilder
            .Entity<JobPost>()
            .HasIndex(x => new
            {
                x.CompanyId,
                x.IsClosed,
                x.CreatedAt,
                x.Id,
            })
            .IsDescending(false, false, true, false)
            .IncludeProperties(x => new { x.ExpiresAt, x.Title });
    }
}
