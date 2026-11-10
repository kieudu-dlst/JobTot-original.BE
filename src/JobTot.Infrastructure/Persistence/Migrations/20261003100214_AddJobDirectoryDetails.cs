using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobDirectoryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Benefits",
                table: "Jobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "Jobs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "ExperienceLevel",
                table: "Jobs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PositionLevel",
                table: "Jobs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "SalaryNote",
                table: "Jobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "SalaryPeriod",
                table: "Jobs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "monthly"
            );

            migrationBuilder.AddColumn<string>(
                name: "WorkSchedule",
                table: "Jobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Benefits", table: "Jobs");

            migrationBuilder.DropColumn(name: "EmploymentType", table: "Jobs");

            migrationBuilder.DropColumn(name: "ExperienceLevel", table: "Jobs");

            migrationBuilder.DropColumn(name: "PositionLevel", table: "Jobs");

            migrationBuilder.DropColumn(name: "SalaryNote", table: "Jobs");

            migrationBuilder.DropColumn(name: "SalaryPeriod", table: "Jobs");

            migrationBuilder.DropColumn(name: "WorkSchedule", table: "Jobs");
        }
    }
}
