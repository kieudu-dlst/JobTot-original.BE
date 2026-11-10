using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateJobDetailsAndSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgeRequirement",
                table: "Jobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "GenderRequirement",
                table: "Jobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "HiringProcess",
                table: "Jobs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "RecruiterEmail",
                table: "Jobs",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "RecruiterName",
                table: "Jobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "RecruiterTitle",
                table: "Jobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "Jobs",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Vacancies",
                table: "Jobs",
                type: "int",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "CandidateJobSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CvId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Phone = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    CoverLetter = table.Column<string>(
                        type: "nvarchar(4000)",
                        maxLength: 4000,
                        nullable: true
                    ),
                    CvTitle = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    CvContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                    Status = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateJobSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateJobSubmissions_CandidateCv_CvId",
                        column: x => x.CvId,
                        principalTable: "CandidateCv",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_CandidateJobSubmissions_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_CandidateJobSubmissions_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateJobSubmissions_CandidateId",
                table: "CandidateJobSubmissions",
                column: "CandidateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateJobSubmissions_CvId",
                table: "CandidateJobSubmissions",
                column: "CvId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateJobSubmissions_JobId_CandidateId",
                table: "CandidateJobSubmissions",
                columns: new[] { "JobId", "CandidateId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CandidateJobSubmissions");

            migrationBuilder.DropColumn(name: "AgeRequirement", table: "Jobs");

            migrationBuilder.DropColumn(name: "GenderRequirement", table: "Jobs");

            migrationBuilder.DropColumn(name: "HiringProcess", table: "Jobs");

            migrationBuilder.DropColumn(name: "RecruiterEmail", table: "Jobs");

            migrationBuilder.DropColumn(name: "RecruiterName", table: "Jobs");

            migrationBuilder.DropColumn(name: "RecruiterTitle", table: "Jobs");

            migrationBuilder.DropColumn(name: "Requirements", table: "Jobs");

            migrationBuilder.DropColumn(name: "Vacancies", table: "Jobs");
        }
    }
}
