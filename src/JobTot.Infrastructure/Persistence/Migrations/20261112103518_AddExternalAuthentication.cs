using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProviderSubject",
                table: "ExternalLogin",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateTable(
                name: "ExternalAuthFlows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrowserSecretHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedData = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthFlows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthFlows_ExpiresAt",
                table: "ExternalAuthFlows",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalAuthFlows");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderSubject",
                table: "ExternalLogin",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldCollation: "Latin1_General_100_BIN2");
        }
    }
}
