using JobTot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JobTot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RecruitmentDbContext))]
[Migration("20261125010000_AddAiChatHistory")]
public partial class AddAiChatHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("AiChatMessages", columns: table => new
        {
            Id = table.Column<long>("bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            AccountId = table.Column<Guid>("uniqueidentifier", nullable: false),
            ConversationId = table.Column<Guid>("uniqueidentifier", nullable: false),
            TurnId = table.Column<Guid>("uniqueidentifier", nullable: false),
            Role = table.Column<string>("nvarchar(16)", maxLength: 16, nullable: false),
            Content = table.Column<string>("nvarchar(max)", nullable: false),
            State = table.Column<string>("nvarchar(16)", maxLength: 16, nullable: false),
            ImageName = table.Column<string>("nvarchar(80)", maxLength: 80, nullable: true),
            ImageMimeType = table.Column<string>("nvarchar(32)", maxLength: 32, nullable: true),
            CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
            UpdatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AiChatMessages", row => row.Id);
            table.ForeignKey("FK_AiChatMessages_Account_AccountId", row => row.AccountId, "Account", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_AiChatMessages_AccountId_ConversationId_Id", "AiChatMessages", new[] { "AccountId", "ConversationId", "Id" });
        migrationBuilder.CreateIndex("IX_AiChatMessages_TurnId_Role", "AiChatMessages", new[] { "TurnId", "Role" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("AiChatMessages");
}
