using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountEmailChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountEmailChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(
                        type: "nvarchar(320)",
                        maxLength: 320,
                        nullable: false
                    ),
                    Role = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    Purpose = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PasswordStamp = table.Column<string>(
                        type: "nvarchar(64)",
                        maxLength: 64,
                        nullable: true
                    ),
                    SecretHash = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                    ExpiresAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                    VerifiedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    ConsumedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountEmailChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountEmailChallenges_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountEmailChallenges_AccountId",
                table: "AccountEmailChallenges",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountEmailChallenges_Email_Role_Purpose_CreatedAt",
                table: "AccountEmailChallenges",
                columns: new[] { "Email", "Role", "Purpose", "CreatedAt" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AccountEmailChallenges");
        }
    }
}
