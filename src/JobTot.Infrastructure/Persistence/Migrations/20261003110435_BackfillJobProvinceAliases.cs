using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillJobProvinceAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Match only a unique catalog name, including common administrative-prefix aliases.
            migrationBuilder.Sql(
                """
                UPDATE job SET ProvinceId = matched.Id
                FROM dbo.Jobs job
                CROSS APPLY (
                    SELECT MIN(CONVERT(char(36), province.Id)) AS Id
                    FROM dbo.Province province
                    WHERE province.Status = 1 AND (
                        province.ProvinceName = job.Location
                        OR province.ProvinceName = N'Thành phố ' + job.Location
                        OR province.ProvinceName = N'Tỉnh ' + job.Location
                        OR (job.Location LIKE N'TP. %' AND province.ProvinceName = N'Thành phố ' + SUBSTRING(job.Location, 5, 200)))
                    HAVING COUNT(*) = 1
                ) matched
                WHERE job.ProvinceId IS NULL;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
