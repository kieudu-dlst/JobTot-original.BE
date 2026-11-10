using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerArticleCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverContentType",
                table: "CareerArticles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "CoverFileName",
                table: "CareerArticles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true
            );

            migrationBuilder.AddColumn<long>(
                name: "CoverSize",
                table: "CareerArticles",
                type: "bigint",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                table: "CareerArticles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CoverContentType", table: "CareerArticles");

            migrationBuilder.DropColumn(name: "CoverFileName", table: "CareerArticles");

            migrationBuilder.DropColumn(name: "CoverSize", table: "CareerArticles");

            migrationBuilder.DropColumn(name: "CoverUrl", table: "CareerArticles");
        }
    }
}
