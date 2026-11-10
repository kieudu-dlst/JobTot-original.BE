using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReadQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecruitmentCampaign_CompanyId",
                table: "RecruitmentCampaign"
            );

            migrationBuilder.DropIndex(
                name: "IX_RecruiterProfile_AccountId",
                table: "RecruiterProfile"
            );

            migrationBuilder.DropIndex(name: "IX_Jobs_CompanyId", table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_CandidateJobSubmissions_CandidateId",
                table: "CandidateJobSubmissions"
            );

            migrationBuilder.DropIndex(name: "IX_Application_CampaignId", table: "Application");

            migrationBuilder.DropIndex(name: "IX_Application_CandidateId", table: "Application");

            migrationBuilder
                .CreateIndex(
                    name: "IX_RecruitmentCampaign_CompanyId_Status_CreatedDate_Id",
                    table: "RecruitmentCampaign",
                    columns: new[] { "CompanyId", "Status", "CreatedDate", "Id" },
                    descending: new[] { false, false, true, false }
                )
                .Annotation("SqlServer:Include", new[] { "CampaignStatus" });

            migrationBuilder
                .CreateIndex(
                    name: "IX_RecruiterProfile_AccountId_Status_JoinedAt_Id",
                    table: "RecruiterProfile",
                    columns: new[] { "AccountId", "Status", "JoinedAt", "Id" }
                )
                .Annotation("SqlServer:Include", new[] { "CompanyId" });

            migrationBuilder
                .CreateIndex(
                    name: "IX_Jobs_CompanyId_IsClosed_CreatedAt_Id",
                    table: "Jobs",
                    columns: new[] { "CompanyId", "IsClosed", "CreatedAt", "Id" },
                    descending: new[] { false, false, true, false }
                )
                .Annotation("SqlServer:Include", new[] { "ExpiresAt", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSavedJobs_CandidateId_SavedAt_JobId",
                table: "CandidateSavedJobs",
                columns: new[] { "CandidateId", "SavedAt", "JobId" },
                descending: new[] { false, true, false }
            );

            migrationBuilder
                .CreateIndex(
                    name: "IX_CandidateJobSubmissions_CandidateId_AppliedAt_Id",
                    table: "CandidateJobSubmissions",
                    columns: new[] { "CandidateId", "AppliedAt", "Id" },
                    descending: new[] { false, true, false }
                )
                .Annotation(
                    "SqlServer:Include",
                    new[] { "JobId", "Status", "ReviewedAt", "CvTitle" }
                );

            migrationBuilder
                .CreateIndex(
                    name: "IX_Application_CampaignId_Status_AppliedAt_Id",
                    table: "Application",
                    columns: new[] { "CampaignId", "Status", "AppliedAt", "Id" },
                    descending: new[] { false, false, true, false }
                )
                .Annotation(
                    "SqlServer:Include",
                    new[] { "CandidateId", "ApplicationStatus", "Source" }
                );

            migrationBuilder
                .CreateIndex(
                    name: "IX_Application_CandidateId_Status_AppliedAt_Id",
                    table: "Application",
                    columns: new[] { "CandidateId", "Status", "AppliedAt", "Id" },
                    descending: new[] { false, false, true, false }
                )
                .Annotation("SqlServer:Include", new[] { "CampaignId", "ApplicationStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecruitmentCampaign_CompanyId_Status_CreatedDate_Id",
                table: "RecruitmentCampaign"
            );

            migrationBuilder.DropIndex(
                name: "IX_RecruiterProfile_AccountId_Status_JoinedAt_Id",
                table: "RecruiterProfile"
            );

            migrationBuilder.DropIndex(
                name: "IX_Jobs_CompanyId_IsClosed_CreatedAt_Id",
                table: "Jobs"
            );

            migrationBuilder.DropIndex(
                name: "IX_CandidateSavedJobs_CandidateId_SavedAt_JobId",
                table: "CandidateSavedJobs"
            );

            migrationBuilder.DropIndex(
                name: "IX_CandidateJobSubmissions_CandidateId_AppliedAt_Id",
                table: "CandidateJobSubmissions"
            );

            migrationBuilder.DropIndex(
                name: "IX_Application_CampaignId_Status_AppliedAt_Id",
                table: "Application"
            );

            migrationBuilder.DropIndex(
                name: "IX_Application_CandidateId_Status_AppliedAt_Id",
                table: "Application"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_CompanyId",
                table: "RecruitmentCampaign",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterProfile_AccountId",
                table: "RecruiterProfile",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CompanyId",
                table: "Jobs",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateJobSubmissions_CandidateId",
                table: "CandidateJobSubmissions",
                column: "CandidateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Application_CampaignId",
                table: "Application",
                column: "CampaignId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Application_CandidateId",
                table: "Application",
                column: "CandidateId"
            );
        }
    }
}
