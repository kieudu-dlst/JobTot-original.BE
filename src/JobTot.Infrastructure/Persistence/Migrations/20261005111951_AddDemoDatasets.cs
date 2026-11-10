using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoDatasets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF OBJECT_ID(N'dbo.DemoDatasets', N'U') IS NULL CREATE TABLE dbo.DemoDatasets ([Key] nvarchar(100) NOT NULL PRIMARY KEY, Payload nvarchar(max) NOT NULL CHECK (ISJSON(Payload)=1));"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DemoDatasets");
        }
    }
}
