using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobProvince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProvinceId",
                table: "Jobs",
                type: "uniqueidentifier",
                nullable: true
            );

            // Only map unambiguous full catalog names. Never infer the work location from the company office.
            migrationBuilder.Sql(
                """
                UPDATE job SET ProvinceId = province.Id
                FROM dbo.Jobs job
                JOIN dbo.Province province ON province.ProvinceName = job.Location AND province.Status = 1
                WHERE job.ProvinceId IS NULL AND NOT EXISTS (
                    SELECT 1 FROM dbo.Province duplicate
                    WHERE duplicate.ProvinceName = province.ProvinceName AND duplicate.Status = 1
                        AND duplicate.Id <> province.Id);
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ProvinceId_IsClosed_ExpiresAt",
                table: "Jobs",
                columns: new[] { "ProvinceId", "IsClosed", "ExpiresAt" }
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Province_ProvinceId",
                table: "Jobs",
                column: "ProvinceId",
                principalTable: "Province",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Jobs_Province_ProvinceId", table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_ProvinceId_IsClosed_ExpiresAt",
                table: "Jobs"
            );

            migrationBuilder.DropColumn(name: "ProvinceId", table: "Jobs");
        }
    }
}
