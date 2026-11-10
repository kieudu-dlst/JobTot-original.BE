using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JobTot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RecruitmentDbContext))]
[Migration("20261008090000_AddPartTimeEmploymentType")]
public sealed class AddPartTimeEmploymentType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM dbo.EmploymentType WHERE TypeName = N'Bán thời gian')
                INSERT INTO dbo.EmploymentType (Id, TypeName, Status, CreatedDate)
                VALUES ('7b71bd12-652d-4c87-a117-696623aef94c', N'Bán thời gian', 1, SYSDATETIMEOFFSET());
            """
        );

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Retain catalog data because recruitment campaigns may reference it.
    }
}
