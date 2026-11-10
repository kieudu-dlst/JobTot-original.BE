using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddErdModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "Name", table: "Companies", newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Companies",
                newName: "About"
            );

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "Companies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "CompanySizeMax",
                table: "Companies",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "CompanySizeMin",
                table: "Companies",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "CoverFileId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedAccountId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedDate",
                table: "Companies",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()"
            );

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Companies",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<int>(
                name: "FoundedYear",
                table: "Companies",
                type: "int",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "HeadOfficeProvinceId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "IndustryId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Companies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "LogoFileId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Companies",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Companies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Companies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedAccountId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedDate",
                table: "Companies",
                type: "datetimeoffset",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "Companies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Companies",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(
                        type: "nvarchar(320)",
                        maxLength: 320,
                        nullable: false
                    ),
                    Phone = table.Column<string>(
                        type: "nvarchar(32)",
                        maxLength: 32,
                        nullable: true
                    ),
                    PasswordHash = table.Column<string>(
                        type: "nvarchar(1024)",
                        maxLength: 1024,
                        nullable: true
                    ),
                    FullName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    AccountType = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    AccountStatus = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    EmailVerifiedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Account_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Account_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ArticleCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Slug = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleCategory_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ArticleCategory_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Benefit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BenefitName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Benefit_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Benefit_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CvCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CvCategory_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CvCategory_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmploymentType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentType_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_EmploymentType_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ExternalLogin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    ProviderSubject = table.Column<string>(
                        type: "nvarchar(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogin", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogin_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ExternalLogin_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ExternalLogin_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Industry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndustryName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Industry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Industry_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Industry_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(
                        type: "nvarchar(2048)",
                        maxLength: 2048,
                        nullable: false
                    ),
                    Type = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Media_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Media_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Province",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvinceName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    ProvinceCode = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Province", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Province_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Province_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RecruiterProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTitle = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    WorkPhone = table.Column<string>(
                        type: "nvarchar(32)",
                        maxLength: 32,
                        nullable: true
                    ),
                    IsCompanyAdmin = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruiterProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruiterProfile_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruiterProfile_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruiterProfile_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruiterProfile_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Skill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skill_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Skill_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TalentTag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalentTag_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentTag_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentTag_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Article",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleCategoryId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false
                    ),
                    AuthorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Slug = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverUrl = table.Column<string>(
                        type: "nvarchar(2048)",
                        maxLength: 2048,
                        nullable: true
                    ),
                    PublishedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Article", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Article_Account_AuthorAccountId",
                        column: x => x.AuthorAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Article_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Article_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Article_ArticleCategory_ArticleCategoryId",
                        column: x => x.ArticleCategoryId,
                        principalTable: "ArticleCategory",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CvTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviewUrl = table.Column<string>(
                        type: "nvarchar(2048)",
                        maxLength: 2048,
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CvTemplate_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CvTemplate_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CvTemplate_CvCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CvCategory",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CompanyIndustry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndustryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyIndustry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyIndustry_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CompanyIndustry_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CompanyIndustry_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CompanyIndustry_Industry_IndustryId",
                        column: x => x.IndustryId,
                        principalTable: "Industry",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CompanyMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyMedia_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CompanyMedia_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CompanyMedia_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CompanyMedia_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CandidateProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Headline = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    YearsExperience = table.Column<int>(type: "int", nullable: true),
                    ExpectedSalaryMin = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true
                    ),
                    ExpectedSalaryMax = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true
                    ),
                    ReadyStatus = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateProfile_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateProfile_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateProfile_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateProfile_Province_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Province",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RecruitmentCampaign",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerRecruiterId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false
                    ),
                    IndustryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentTypeId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false
                    ),
                    Title = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Department = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkAddress = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    WorkMode = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Seniority = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    EducationLevel = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    ExperienceMinYears = table.Column<int>(type: "int", nullable: true),
                    ExperienceMaxYears = table.Column<int>(type: "int", nullable: true),
                    SalaryMin = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true
                    ),
                    SalaryMax = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true
                    ),
                    SalaryVisible = table.Column<bool>(type: "bit", nullable: false),
                    AgeMin = table.Column<int>(type: "int", nullable: true),
                    AgeMax = table.Column<int>(type: "int", nullable: true),
                    GenderPreference = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Openings = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CampaignStatus = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Slug = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    PublishedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    ExpiresAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    PostingStatus = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentCampaign", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaign_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaign_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaign_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaign_EmploymentType_EmploymentTypeId",
                        column: x => x.EmploymentTypeId,
                        principalTable: "EmploymentType",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaign_Industry_IndustryId",
                        column: x => x.IndustryId,
                        principalTable: "Industry",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaign_Province_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Province",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaign_RecruiterProfile_OwnerRecruiterId",
                        column: x => x.OwnerRecruiterId,
                        principalTable: "RecruiterProfile",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CandidateCv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CvTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    FileUrl = table.Column<string>(
                        type: "nvarchar(2048)",
                        maxLength: 2048,
                        nullable: true
                    ),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCv", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateCv_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateCv_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateCv_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateCv_CvTemplate_CvTemplateId",
                        column: x => x.CvTemplateId,
                        principalTable: "CvTemplate",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CandidateSkill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proficiency = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateSkill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateSkill_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateSkill_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateSkill_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CandidateSkill_Skill_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skill",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "FollowedCompany",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowedCompany", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowedCompany_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_FollowedCompany_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_FollowedCompany_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_FollowedCompany_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TalentPoolEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedByRecruiterId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false
                    ),
                    ReadinessStatus = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentPoolEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalentPoolEntry_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentPoolEntry_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentPoolEntry_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentPoolEntry_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentPoolEntry_RecruiterProfile_AddedByRecruiterId",
                        column: x => x.AddedByRecruiterId,
                        principalTable: "RecruiterProfile",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CampaignBenefit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BenefitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignBenefit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignBenefit_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CampaignBenefit_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CampaignBenefit_Benefit_BenefitId",
                        column: x => x.BenefitId,
                        principalTable: "Benefit",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CampaignBenefit_RecruitmentCampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "RecruitmentCampaign",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CampaignSkill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignSkill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignSkill_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CampaignSkill_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CampaignSkill_RecruitmentCampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "RecruitmentCampaign",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CampaignSkill_Skill_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skill",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CampaignStage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StageName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    StageOrder = table.Column<int>(type: "int", nullable: false),
                    StageKind = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignStage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignStage_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CampaignStage_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_CampaignStage_RecruitmentCampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "RecruitmentCampaign",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SavedJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecruitmentCampaignId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false
                    ),
                    SavedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedJob", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedJob_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_SavedJob_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_SavedJob_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_SavedJob_RecruitmentCampaign_RecruitmentCampaignId",
                        column: x => x.RecruitmentCampaignId,
                        principalTable: "RecruitmentCampaign",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TalentPoolTag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TalentPoolEntryId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false
                    ),
                    TalentTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentPoolTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalentPoolTag_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentPoolTag_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentPoolTag_TalentPoolEntry_TalentPoolEntryId",
                        column: x => x.TalentPoolEntryId,
                        principalTable: "TalentPoolEntry",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_TalentPoolTag_TalentTag_TalentTagId",
                        column: x => x.TalentTagId,
                        principalTable: "TalentTag",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Application",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CvId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    ApplicationStatus = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    AppliedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                    CoverLetter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchScore = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Application", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Application_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Application_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Application_CampaignStage_StageId",
                        column: x => x.StageId,
                        principalTable: "CampaignStage",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Application_CandidateCv_CvId",
                        column: x => x.CvId,
                        principalTable: "CandidateCv",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Application_CandidateProfile_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfile",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Application_RecruitmentCampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "RecruitmentCampaign",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ApplicationStageHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByRecruiterId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false
                    ),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStageHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationStageHistory_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ApplicationStageHistory_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ApplicationStageHistory_Application_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Application",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ApplicationStageHistory_CampaignStage_StageId",
                        column: x => x.StageId,
                        principalTable: "CampaignStage",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ApplicationStageHistory_RecruiterProfile_ChangedByRecruiterId",
                        column: x => x.ChangedByRecruiterId,
                        principalTable: "RecruiterProfile",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Interview",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledStart = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                    ScheduledEnd = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false
                    ),
                    MeetingUrl = table.Column<string>(
                        type: "nvarchar(2048)",
                        maxLength: 2048,
                        nullable: true
                    ),
                    Location = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    InterviewStatus = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interview", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interview_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Interview_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Interview_Application_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Application",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Interview_CampaignStage_StageId",
                        column: x => x.StageId,
                        principalTable: "CampaignStage",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "InterviewFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecruiterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: true
                    ),
                    Decision = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false,
                        defaultValueSql: "SYSDATETIMEOFFSET()"
                    ),
                    CreatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: true
                    ),
                    UpdatedAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewFeedback_Account_CreatedAccountId",
                        column: x => x.CreatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_InterviewFeedback_Account_UpdatedAccountId",
                        column: x => x.UpdatedAccountId,
                        principalTable: "Account",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_InterviewFeedback_Interview_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interview",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_InterviewFeedback_RecruiterProfile_RecruiterId",
                        column: x => x.RecruiterId,
                        principalTable: "RecruiterProfile",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CoverFileId",
                table: "Companies",
                column: "CoverFileId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CreatedAccountId",
                table: "Companies",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Companies_HeadOfficeProvinceId",
                table: "Companies",
                column: "HeadOfficeProvinceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IndustryId",
                table: "Companies",
                column: "IndustryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Companies_LogoFileId",
                table: "Companies",
                column: "LogoFileId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Companies_UpdatedAccountId",
                table: "Companies",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Account_CreatedAccountId",
                table: "Account",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Account_Email",
                table: "Account",
                column: "Email",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Account_UpdatedAccountId",
                table: "Account",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Application_CampaignId",
                table: "Application",
                column: "CampaignId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Application_CandidateId",
                table: "Application",
                column: "CandidateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Application_CreatedAccountId",
                table: "Application",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Application_CvId",
                table: "Application",
                column: "CvId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Application_StageId",
                table: "Application",
                column: "StageId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Application_UpdatedAccountId",
                table: "Application",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStageHistory_ApplicationId",
                table: "ApplicationStageHistory",
                column: "ApplicationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStageHistory_ChangedByRecruiterId",
                table: "ApplicationStageHistory",
                column: "ChangedByRecruiterId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStageHistory_CreatedAccountId",
                table: "ApplicationStageHistory",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStageHistory_StageId",
                table: "ApplicationStageHistory",
                column: "StageId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStageHistory_UpdatedAccountId",
                table: "ApplicationStageHistory",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Article_ArticleCategoryId",
                table: "Article",
                column: "ArticleCategoryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Article_AuthorAccountId",
                table: "Article",
                column: "AuthorAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Article_CreatedAccountId",
                table: "Article",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Article_UpdatedAccountId",
                table: "Article",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ArticleCategory_CreatedAccountId",
                table: "ArticleCategory",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ArticleCategory_UpdatedAccountId",
                table: "ArticleCategory",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Benefit_CreatedAccountId",
                table: "Benefit",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Benefit_UpdatedAccountId",
                table: "Benefit",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignBenefit_BenefitId",
                table: "CampaignBenefit",
                column: "BenefitId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignBenefit_CampaignId_BenefitId",
                table: "CampaignBenefit",
                columns: new[] { "CampaignId", "BenefitId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignBenefit_CreatedAccountId",
                table: "CampaignBenefit",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignBenefit_UpdatedAccountId",
                table: "CampaignBenefit",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignSkill_CampaignId_SkillId",
                table: "CampaignSkill",
                columns: new[] { "CampaignId", "SkillId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignSkill_CreatedAccountId",
                table: "CampaignSkill",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignSkill_SkillId",
                table: "CampaignSkill",
                column: "SkillId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignSkill_UpdatedAccountId",
                table: "CampaignSkill",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignStage_CampaignId",
                table: "CampaignStage",
                column: "CampaignId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignStage_CreatedAccountId",
                table: "CampaignStage",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CampaignStage_UpdatedAccountId",
                table: "CampaignStage",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCv_CandidateId",
                table: "CandidateCv",
                column: "CandidateId",
                unique: true,
                filter: "[IsDefault] = 1"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCv_CreatedAccountId",
                table: "CandidateCv",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCv_CvTemplateId",
                table: "CandidateCv",
                column: "CvTemplateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCv_UpdatedAccountId",
                table: "CandidateCv",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfile_AccountId",
                table: "CandidateProfile",
                column: "AccountId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfile_CreatedAccountId",
                table: "CandidateProfile",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfile_ProvinceId",
                table: "CandidateProfile",
                column: "ProvinceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfile_UpdatedAccountId",
                table: "CandidateProfile",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSkill_CandidateId_SkillId",
                table: "CandidateSkill",
                columns: new[] { "CandidateId", "SkillId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSkill_CreatedAccountId",
                table: "CandidateSkill",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSkill_SkillId",
                table: "CandidateSkill",
                column: "SkillId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSkill_UpdatedAccountId",
                table: "CandidateSkill",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyIndustry_CompanyId_IndustryId",
                table: "CompanyIndustry",
                columns: new[] { "CompanyId", "IndustryId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyIndustry_CreatedAccountId",
                table: "CompanyIndustry",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyIndustry_IndustryId",
                table: "CompanyIndustry",
                column: "IndustryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyIndustry_UpdatedAccountId",
                table: "CompanyIndustry",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMedia_CompanyId_MediaId",
                table: "CompanyMedia",
                columns: new[] { "CompanyId", "MediaId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMedia_CreatedAccountId",
                table: "CompanyMedia",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMedia_MediaId",
                table: "CompanyMedia",
                column: "MediaId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMedia_UpdatedAccountId",
                table: "CompanyMedia",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CvCategory_CreatedAccountId",
                table: "CvCategory",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CvCategory_UpdatedAccountId",
                table: "CvCategory",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CvTemplate_CategoryId",
                table: "CvTemplate",
                column: "CategoryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CvTemplate_CreatedAccountId",
                table: "CvTemplate",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CvTemplate_UpdatedAccountId",
                table: "CvTemplate",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentType_CreatedAccountId",
                table: "EmploymentType",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentType_UpdatedAccountId",
                table: "EmploymentType",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogin_AccountId",
                table: "ExternalLogin",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogin_CreatedAccountId",
                table: "ExternalLogin",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogin_Provider_ProviderSubject",
                table: "ExternalLogin",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogin_UpdatedAccountId",
                table: "ExternalLogin",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_FollowedCompany_CandidateId_CompanyId",
                table: "FollowedCompany",
                columns: new[] { "CandidateId", "CompanyId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_FollowedCompany_CompanyId",
                table: "FollowedCompany",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_FollowedCompany_CreatedAccountId",
                table: "FollowedCompany",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_FollowedCompany_UpdatedAccountId",
                table: "FollowedCompany",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Industry_CreatedAccountId",
                table: "Industry",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Industry_UpdatedAccountId",
                table: "Industry",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Interview_ApplicationId",
                table: "Interview",
                column: "ApplicationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Interview_CreatedAccountId",
                table: "Interview",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Interview_StageId",
                table: "Interview",
                column: "StageId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Interview_UpdatedAccountId",
                table: "Interview",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedback_CreatedAccountId",
                table: "InterviewFeedback",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedback_InterviewId",
                table: "InterviewFeedback",
                column: "InterviewId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedback_RecruiterId",
                table: "InterviewFeedback",
                column: "RecruiterId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedback_UpdatedAccountId",
                table: "InterviewFeedback",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Media_CreatedAccountId",
                table: "Media",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Media_UpdatedAccountId",
                table: "Media",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Province_CreatedAccountId",
                table: "Province",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Province_UpdatedAccountId",
                table: "Province",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterProfile_AccountId",
                table: "RecruiterProfile",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterProfile_CompanyId",
                table: "RecruiterProfile",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterProfile_CreatedAccountId",
                table: "RecruiterProfile",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterProfile_UpdatedAccountId",
                table: "RecruiterProfile",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_CompanyId",
                table: "RecruitmentCampaign",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_CreatedAccountId",
                table: "RecruitmentCampaign",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_EmploymentTypeId",
                table: "RecruitmentCampaign",
                column: "EmploymentTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_IndustryId",
                table: "RecruitmentCampaign",
                column: "IndustryId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_OwnerRecruiterId",
                table: "RecruitmentCampaign",
                column: "OwnerRecruiterId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_ProvinceId",
                table: "RecruitmentCampaign",
                column: "ProvinceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaign_UpdatedAccountId",
                table: "RecruitmentCampaign",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SavedJob_CandidateId_RecruitmentCampaignId",
                table: "SavedJob",
                columns: new[] { "CandidateId", "RecruitmentCampaignId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SavedJob_CreatedAccountId",
                table: "SavedJob",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SavedJob_RecruitmentCampaignId",
                table: "SavedJob",
                column: "RecruitmentCampaignId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SavedJob_UpdatedAccountId",
                table: "SavedJob",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Skill_CreatedAccountId",
                table: "Skill",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Skill_UpdatedAccountId",
                table: "Skill",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolEntry_AddedByRecruiterId",
                table: "TalentPoolEntry",
                column: "AddedByRecruiterId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolEntry_CandidateId",
                table: "TalentPoolEntry",
                column: "CandidateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolEntry_CompanyId",
                table: "TalentPoolEntry",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolEntry_CreatedAccountId",
                table: "TalentPoolEntry",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolEntry_UpdatedAccountId",
                table: "TalentPoolEntry",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolTag_CreatedAccountId",
                table: "TalentPoolTag",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolTag_TalentPoolEntryId_TalentTagId",
                table: "TalentPoolTag",
                columns: new[] { "TalentPoolEntryId", "TalentTagId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolTag_TalentTagId",
                table: "TalentPoolTag",
                column: "TalentTagId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolTag_UpdatedAccountId",
                table: "TalentPoolTag",
                column: "UpdatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentTag_CompanyId",
                table: "TalentTag",
                column: "CompanyId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentTag_CreatedAccountId",
                table: "TalentTag",
                column: "CreatedAccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TalentTag_UpdatedAccountId",
                table: "TalentTag",
                column: "UpdatedAccountId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Account_CreatedAccountId",
                table: "Companies",
                column: "CreatedAccountId",
                principalTable: "Account",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Account_UpdatedAccountId",
                table: "Companies",
                column: "UpdatedAccountId",
                principalTable: "Account",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Industry_IndustryId",
                table: "Companies",
                column: "IndustryId",
                principalTable: "Industry",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Media_CoverFileId",
                table: "Companies",
                column: "CoverFileId",
                principalTable: "Media",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Media_LogoFileId",
                table: "Companies",
                column: "LogoFileId",
                principalTable: "Media",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Province_HeadOfficeProvinceId",
                table: "Companies",
                column: "HeadOfficeProvinceId",
                principalTable: "Province",
                principalColumn: "Id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Account_CreatedAccountId",
                table: "Companies"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Account_UpdatedAccountId",
                table: "Companies"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Industry_IndustryId",
                table: "Companies"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Media_CoverFileId",
                table: "Companies"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Media_LogoFileId",
                table: "Companies"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Province_HeadOfficeProvinceId",
                table: "Companies"
            );

            migrationBuilder.DropTable(name: "ApplicationStageHistory");

            migrationBuilder.DropTable(name: "Article");

            migrationBuilder.DropTable(name: "CampaignBenefit");

            migrationBuilder.DropTable(name: "CampaignSkill");

            migrationBuilder.DropTable(name: "CandidateSkill");

            migrationBuilder.DropTable(name: "CompanyIndustry");

            migrationBuilder.DropTable(name: "CompanyMedia");

            migrationBuilder.DropTable(name: "ExternalLogin");

            migrationBuilder.DropTable(name: "FollowedCompany");

            migrationBuilder.DropTable(name: "InterviewFeedback");

            migrationBuilder.DropTable(name: "SavedJob");

            migrationBuilder.DropTable(name: "TalentPoolTag");

            migrationBuilder.DropTable(name: "ArticleCategory");

            migrationBuilder.DropTable(name: "Benefit");

            migrationBuilder.DropTable(name: "Skill");

            migrationBuilder.DropTable(name: "Media");

            migrationBuilder.DropTable(name: "Interview");

            migrationBuilder.DropTable(name: "TalentPoolEntry");

            migrationBuilder.DropTable(name: "TalentTag");

            migrationBuilder.DropTable(name: "Application");

            migrationBuilder.DropTable(name: "CampaignStage");

            migrationBuilder.DropTable(name: "CandidateCv");

            migrationBuilder.DropTable(name: "RecruitmentCampaign");

            migrationBuilder.DropTable(name: "CandidateProfile");

            migrationBuilder.DropTable(name: "CvTemplate");

            migrationBuilder.DropTable(name: "EmploymentType");

            migrationBuilder.DropTable(name: "Industry");

            migrationBuilder.DropTable(name: "RecruiterProfile");

            migrationBuilder.DropTable(name: "Province");

            migrationBuilder.DropTable(name: "CvCategory");

            migrationBuilder.DropTable(name: "Account");

            migrationBuilder.DropIndex(name: "IX_Companies_CoverFileId", table: "Companies");

            migrationBuilder.DropIndex(name: "IX_Companies_CreatedAccountId", table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_HeadOfficeProvinceId",
                table: "Companies"
            );

            migrationBuilder.DropIndex(name: "IX_Companies_IndustryId", table: "Companies");

            migrationBuilder.DropIndex(name: "IX_Companies_LogoFileId", table: "Companies");

            migrationBuilder.DropIndex(name: "IX_Companies_UpdatedAccountId", table: "Companies");

            migrationBuilder.DropColumn(name: "AddressLine", table: "Companies");

            migrationBuilder.DropColumn(name: "CompanySizeMax", table: "Companies");

            migrationBuilder.DropColumn(name: "CompanySizeMin", table: "Companies");

            migrationBuilder.DropColumn(name: "CoverFileId", table: "Companies");

            migrationBuilder.DropColumn(name: "CreatedAccountId", table: "Companies");

            migrationBuilder.DropColumn(name: "CreatedDate", table: "Companies");

            migrationBuilder.DropColumn(name: "Culture", table: "Companies");

            migrationBuilder.DropColumn(name: "Email", table: "Companies");

            migrationBuilder.DropColumn(name: "FoundedYear", table: "Companies");

            migrationBuilder.DropColumn(name: "HeadOfficeProvinceId", table: "Companies");

            migrationBuilder.DropColumn(name: "IndustryId", table: "Companies");

            migrationBuilder.DropColumn(name: "LegalName", table: "Companies");

            migrationBuilder.DropColumn(name: "LogoFileId", table: "Companies");

            migrationBuilder.DropColumn(name: "Phone", table: "Companies");

            migrationBuilder.DropColumn(name: "Slug", table: "Companies");

            migrationBuilder.DropColumn(name: "Status", table: "Companies");

            migrationBuilder.DropColumn(name: "TaxCode", table: "Companies");

            migrationBuilder.DropColumn(name: "UpdatedAccountId", table: "Companies");

            migrationBuilder.DropColumn(name: "UpdatedDate", table: "Companies");

            migrationBuilder.DropColumn(name: "VerificationStatus", table: "Companies");

            migrationBuilder.DropColumn(name: "WebsiteUrl", table: "Companies");

            migrationBuilder.RenameColumn(name: "DisplayName", table: "Companies", newName: "Name");

            migrationBuilder.RenameColumn(
                name: "About",
                table: "Companies",
                newName: "Description"
            );
        }
    }
}
