using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountMessageReads",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AccountMessageReads",
                        x => new { x.AccountId, x.MessageId }
                    );
                    table.ForeignKey(
                        name: "FK_AccountMessageReads_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_AccountMessageReads_ChatMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AccountNotificationReads",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AccountNotificationReads",
                        x => new { x.AccountId, x.NotificationId }
                    );
                    table.ForeignKey(
                        name: "FK_AccountNotificationReads_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountMessageReads_MessageId",
                table: "AccountMessageReads",
                column: "MessageId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AccountMessageReads");

            migrationBuilder.DropTable(name: "AccountNotificationReads");
        }
    }
}
