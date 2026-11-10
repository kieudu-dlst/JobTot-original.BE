using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerCandidateInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployerCandidateInterests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterestedByRecruiterId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false
                    ),
                    InterestedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerCandidateInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployerCandidateInterests_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployerCandidateInterests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_EmployerCandidateInterests_RecruiterProfile_InterestedByRecruiterId",
                        column: x => x.InterestedByRecruiterId,
                        principalTable: "RecruiterProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployerCandidateInterests_CandidateId",
                table: "EmployerCandidateInterests",
                column: "CandidateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployerCandidateInterests_CompanyId_CandidateId",
                table: "EmployerCandidateInterests",
                columns: new[] { "CompanyId", "CandidateId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmployerCandidateInterests_InterestedByRecruiterId",
                table: "EmployerCandidateInterests",
                column: "InterestedByRecruiterId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EmployerCandidateInterests");
        }
    }
}
