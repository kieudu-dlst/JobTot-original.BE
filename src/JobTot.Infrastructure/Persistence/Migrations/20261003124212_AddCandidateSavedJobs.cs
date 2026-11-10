using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateSavedJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "CandidateJobSubmissions",
                type: "datetimeoffset",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "CandidateSavedJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateSavedJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateSavedJobs_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_CandidateSavedJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSavedJobs_CandidateId_JobId",
                table: "CandidateSavedJobs",
                columns: new[] { "CandidateId", "JobId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSavedJobs_JobId",
                table: "CandidateSavedJobs",
                column: "JobId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CandidateSavedJobs");

            migrationBuilder.DropColumn(name: "ReviewedAt", table: "CandidateJobSubmissions");
        }
    }
}
