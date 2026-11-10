using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PairKey = table.Column<string>(
                        type: "nvarchar(73)",
                        maxLength: 73,
                        nullable: false
                    ),
                    FirstAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecondAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatConversations_Account_FirstAccountId",
                        column: x => x.FirstAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_ChatConversations_Account_SecondAccountId",
                        column: x => x.SecondAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(
                        type: "nvarchar(4000)",
                        maxLength: 4000,
                        nullable: false
                    ),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Account_SenderAccountId",
                        column: x => x.SenderAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_FirstAccountId",
                table: "ChatConversations",
                column: "FirstAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_PairKey",
                table: "ChatConversations",
                column: "PairKey",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_SecondAccountId",
                table: "ChatConversations",
                column: "SecondAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_Id",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "Id" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderAccountId_ClientMessageId",
                table: "ChatMessages",
                columns: new[] { "SenderAccountId", "ClientMessageId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChatMessages");

            migrationBuilder.DropTable(name: "ChatConversations");
        }
    }
}
