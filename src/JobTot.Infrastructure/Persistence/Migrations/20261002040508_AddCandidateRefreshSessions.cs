using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateRefreshSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateRefreshSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(
                        type: "nvarchar(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    PasswordStamp = table.Column<string>(
                        type: "nvarchar(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    ExpiresAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                    RevokedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateRefreshSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateRefreshSessions_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UsedCandidateRefreshTokens",
                columns: table => new
                {
                    TokenHash = table.Column<string>(
                        type: "nvarchar(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsedCandidateRefreshTokens", x => x.TokenHash);
                    table.ForeignKey(
                        name: "FK_UsedCandidateRefreshTokens_CandidateRefreshSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "CandidateRefreshSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateRefreshSessions_AccountId",
                table: "CandidateRefreshSessions",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UsedCandidateRefreshTokens_SessionId",
                table: "UsedCandidateRefreshTokens",
                column: "SessionId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UsedCandidateRefreshTokens");

            migrationBuilder.DropTable(name: "CandidateRefreshSessions");
        }
    }
}
