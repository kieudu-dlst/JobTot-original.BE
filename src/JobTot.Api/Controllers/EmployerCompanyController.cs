using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JobTot.Application.Companies;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api.Controllers;

[ApiController]
[Authorize(Policy = EmployerAuthentication.Policy)]
[Route("api/employer/company")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmployerCompanyController(RecruitmentDbContext db, CompanyImageStorage images)
    : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("industries")]
    public async Task<IActionResult> Industries(CancellationToken ct) =>
        Ok(
            await db
                .IndustryRecords.AsNoTracking()
                .Where(i => i.Status == EntityStatus.Active)
                .OrderBy(i => i.IndustryName)
                .Select(i => new { i.Id, Name = i.IndustryName })
                .ToListAsync(ct)
        );

    private async Task<RecruiterProfile?> Membership(CancellationToken ct)
    {
        try
        {
            return await (
                db
                    .RecruiterProfileRecords.Include(p => p.Company)
                    .ThenInclude(c => c.HeadOfficeProvince)
                    .Include(p => p.Company)
                    .ThenInclude(c => c.LogoFile)
                    .Include(p => p.Company)
                    .ThenInclude(c => c.CoverFile)
                    .Where(p =>
                        p.AccountId == AccountId
                        && p.Status == EntityStatus.Active
                        && p.Company.Status == EntityStatus.Active
                    )
                    .OrderBy(p => p.JoinedAt)
                    .ThenBy(p => p.Id)
                    .FirstOrDefaultAsync(ct)
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCompanyController.Membership"
            );
            throw;
        }
    }

    private static Task<bool> CanEdit(RecruiterProfile profile, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(
            profile.Status == EntityStatus.Active && profile.Company.Status == EntityStatus.Active
        );
    }

    [HttpGet]
    public async Task<ActionResult<EmployerCompanyResponse>> Get(CancellationToken ct)
    {
        try
        {
            var profile = await Membership(ct);
            return profile is null ? NotFound() : Ok(await ToResponse(profile, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCompanyController.Get"
            );
            throw;
        }
    }

    [HttpPut]
    [RequestSizeLimit(512 * 1024)]
    public async Task<ActionResult<EmployerCompanyResponse>> Save(
        EmployerCompanyDocument document,
        CancellationToken ct
    )
    {
        try
        {
            var profile = await Membership(ct);
            if (profile is null)
                return NotFound();
            if (!await CanEdit(profile, ct))
                return Forbid();
            Validate(document);
            var existingBrand = ReadBrand(profile.Company);
            var industryId = profile.Company.IndustryId;
            if (document.Brand.IndustryIds is { } selected)
            {
                if (selected.Count > 20 || selected.Distinct().Count() != selected.Count)
                    return BadRequest();
                var names = await db
                    .IndustryRecords.Where(i =>
                        selected.Contains(i.Id) && i.Status == EntityStatus.Active
                    )
                    .OrderBy(i => i.IndustryName)
                    .Select(i => i.IndustryName)
                    .ToListAsync(ct);
                if (names.Count != selected.Count)
                    return BadRequest(
                        new ProblemDetails
                        {
                            Detail = "Vui lòng chọn ngành nghề đang hoạt động trong danh sách.",
                        }
                    );
                document.Brand.Industry = string.Join(", ", names);
                industryId = selected.Count == 0 ? null : selected[0];
            }
            else
            {
                if (document.Brand.Industry != existingBrand.Industry)
                {
                    industryId = string.IsNullOrWhiteSpace(document.Brand.Industry)
                        ? null
                        : await db
                            .IndustryRecords.Where(i =>
                                i.Status == EntityStatus.Active
                                && i.IndustryName == document.Brand.Industry
                            )
                            .Select(i => (Guid?)i.Id)
                            .FirstOrDefaultAsync(ct);
                    if (!string.IsNullOrWhiteSpace(document.Brand.Industry) && industryId is null)
                        return BadRequest(
                            new ProblemDetails
                            {
                                Detail = "Vui lòng chọn ngành nghề từ danh sách đang hoạt động.",
                            }
                        );
                }
            }
            if (
                document.Brand.LogoUrl != existingBrand.LogoUrl
                || document.Brand.CoverUrl != existingBrand.CoverUrl
            )
                return BadRequest(
                    new ProblemDetails
                    {
                        Detail = "Vui lòng tải logo và ảnh bìa qua chức năng tải ảnh.",
                    }
                );
            var company = profile.Company;
            var oldCulture = company.Culture;
            if (document.Revision != Revision(company))
                return Conflict(
                    new ProblemDetails
                    {
                        Detail = "Hồ sơ đã thay đổi ở phiên khác. Vui lòng tải lại trước khi lưu.",
                    }
                );
            document.Brand.SchemaVersion = 1;
            document.Brand.UpdateStamp = Guid.NewGuid();
            var culture = JsonSerializer.Serialize(document.Brand, JsonOptions);
            var companyId = company.Id;
            var saved = await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    db.ChangeTracker.Clear();
                    var attemptCompany = await db.Companies.SingleAsync(c => c.Id == companyId, ct);
                    // A previous attempt may have committed before the connection failed.
                    if (attemptCompany.Culture == culture)
                        return true;
                    await using var transaction = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(ct)
                        : null;
                    // A conditional update prevents one editor from overwriting another editor's changes.
                    if (db.Database.IsRelational())
                    {
                        var changed = await db
                            .Companies.Where(c =>
                                c.Id == attemptCompany.Id && c.Culture == oldCulture
                            )
                            .ExecuteUpdateAsync(
                                update =>
                                    update
                                        .SetProperty(c => c.Name, document.Name.Trim())
                                        .SetProperty(c => c.Description, document.Description)
                                        .SetProperty(c => c.AddressLine, document.Address.Trim())
                                        .SetProperty(c => c.WebsiteUrl, document.Website.Trim())
                                        .SetProperty(c => c.FoundedYear, document.FoundedYear)
                                        .SetProperty(c => c.CompanySizeMin, document.SizeMin)
                                        .SetProperty(c => c.CompanySizeMax, document.SizeMax)
                                        .SetProperty(c => c.Culture, culture)
                                        .SetProperty(c => c.IndustryId, industryId),
                                ct
                            );
                        if (changed != 1)
                            return false;
                        await db.Entry(attemptCompany).ReloadAsync(ct);
                    }
                    else
                    {
                        attemptCompany.DisplayName = document.Name.Trim();
                        attemptCompany.About = document.Description;
                        attemptCompany.AddressLine = document.Address.Trim();
                        attemptCompany.WebsiteUrl = document.Website.Trim();
                        attemptCompany.FoundedYear = document.FoundedYear;
                        attemptCompany.CompanySizeMin = document.SizeMin;
                        attemptCompany.CompanySizeMax = document.SizeMax;
                        attemptCompany.Culture = culture;
                        attemptCompany.IndustryId = industryId;
                        await db.SaveChangesAsync(ct);
                    }

                    if (document.Brand.IndustryIds is { } selectedIds)
                    {
                        var links = await db
                            .CompanyIndustryRecords.Where(x => x.CompanyId == attemptCompany.Id)
                            .ToListAsync(ct);
                        foreach (var link in links)
                            link.Status = selectedIds.Contains(link.IndustryId)
                                ? EntityStatus.Active
                                : EntityStatus.Inactive;
                        foreach (
                            var id in selectedIds.Where(id =>
                                !links.Any(link => link.IndustryId == id)
                            )
                        )
                            db.CompanyIndustryRecords.Add(
                                new CompanyIndustry
                                {
                                    Id = Guid.NewGuid(),
                                    CompanyId = attemptCompany.Id,
                                    IndustryId = id,
                                    Status = EntityStatus.Active,
                                }
                            );
                        await db.SaveChangesAsync(ct);
                    }
                    if (transaction is not null)
                        await transaction.CommitAsync(ct);
                    return true;
                });
            if (!saved)
                return Conflict(
                    new ProblemDetails { Detail = "Hồ sơ đã thay đổi. Vui lòng tải lại." }
                );
            return Ok(await ToResponse((await Membership(ct))!, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCompanyController.Save"
            );
            throw;
        }
    }

    [HttpPost("images/{kind}")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<EmployerCompanyResponse>> UploadImage(
        string kind,
        IFormFile file,
        [FromForm] string revision,
        CancellationToken ct
    )
    {
        try
        {
            if (kind is not ("logo" or "cover" or "gallery"))
                return BadRequest();
            var profile = await Membership(ct);
            if (profile is null)
                return NotFound();
            if (!await CanEdit(profile, ct))
                return Forbid();
            if (revision != Revision(profile.Company))
                return Conflict(
                    new ProblemDetails { Detail = "Hồ sơ đã thay đổi. Vui lòng tải lại." }
                );
            if (file.Length is <= 0 or > 5 * 1024 * 1024)
                return BadRequest(new ProblemDetails { Detail = "Ảnh tối đa 5 MB." });
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var png =
                bytes.Length >= 8
                && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var jpeg = bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
            if (
                (!png && !jpeg)
                || (png && file.ContentType != "image/png")
                || (jpeg && file.ContentType != "image/jpeg")
            )
                return BadRequest(new ProblemDetails { Detail = "Chỉ nhận ảnh PNG hoặc JPEG." });
            if (kind == "gallery" && ReadBrand(profile.Company).Gallery.Count >= 12)
                return BadRequest();
            var url = await images.SaveAsync(bytes, kind, png, ct);
            var companyId = profile.CompanyId;
            var oldCulture = profile.Company.Culture;
            var oldLogo = profile.Company.LogoFileId;
            var oldCover = profile.Company.CoverFileId;
            var brand = ReadBrand(profile.Company);
            if (kind == "logo")
                brand.LogoUrl = url;
            else if (kind == "cover")
                brand.CoverUrl = url;
            else
                brand.Gallery.Add(url);
            brand.UpdateStamp = Guid.NewGuid();
            var culture = JsonSerializer.Serialize(brand, JsonOptions);
            var mediaId = Guid.NewGuid();
            var changed = await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(ct)
                        : null;
                    db.ChangeTracker.Clear();
                    db.MediaRecords.Add(
                        new Media
                        {
                            Id = mediaId,
                            Url = url,
                            Type =
                                kind == "logo" ? "CompanyLogo"
                                : kind == "cover" ? "CompanyCover"
                                : "CompanyGallery",
                            CreatedAccountId = AccountId,
                        }
                    );
                    await db.SaveChangesAsync(ct);
                    if (db.Database.IsRelational())
                    {
                        var query = db.Companies.Where(c =>
                            c.Id == companyId
                            && c.Culture == oldCulture
                            && c.LogoFileId == oldLogo
                            && c.CoverFileId == oldCover
                        );
                        var count =
                            kind == "logo"
                                ? await query.ExecuteUpdateAsync(
                                    u =>
                                        u.SetProperty(c => c.LogoFileId, mediaId)
                                            .SetProperty(c => c.Culture, culture)
                                            .SetProperty(c => c.UpdatedAccountId, AccountId)
                                            .SetProperty(c => c.UpdatedDate, DateTimeOffset.UtcNow),
                                    ct
                                )
                            : kind == "gallery"
                                ? await query.ExecuteUpdateAsync(
                                    u =>
                                        u.SetProperty(c => c.Culture, culture)
                                            .SetProperty(c => c.UpdatedAccountId, AccountId)
                                            .SetProperty(c => c.UpdatedDate, DateTimeOffset.UtcNow),
                                    ct
                                )
                            : await query.ExecuteUpdateAsync(
                                u =>
                                    u.SetProperty(c => c.CoverFileId, mediaId)
                                        .SetProperty(c => c.Culture, culture)
                                        .SetProperty(c => c.UpdatedAccountId, AccountId)
                                        .SetProperty(c => c.UpdatedDate, DateTimeOffset.UtcNow),
                                ct
                            );
                        if (count != 1)
                            return false; // Transaction disposal rolls back the media row.
                    }
                    else
                    {
                        var company = await db.Companies.SingleAsync(c => c.Id == companyId, ct);
                        if (kind == "logo")
                            company.LogoFileId = mediaId;
                        else if (kind == "cover")
                            company.CoverFileId = mediaId;
                        company.Culture = culture;
                        await db.SaveChangesAsync(ct);
                    }
                    if (transaction is not null)
                        await transaction.CommitAsync(ct);
                    return true;
                });
            if (!changed)
                return Conflict(
                    new ProblemDetails { Detail = "Hồ sơ đã thay đổi. Vui lòng tải lại." }
                );
            return Ok(await ToResponse((await Membership(ct))!, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                ex,
                "EmployerCompanyController.UploadImage"
            );
            throw;
        }
    }

    private async Task<EmployerCompanyResponse> ToResponse(
        RecruiterProfile profile,
        CancellationToken ct
    )
    {
        try
        {
            var company = profile.Company;
            var brand = ReadBrand(company);
            brand.IndustryIds ??= await db
                .CompanyIndustryRecords.Where(x =>
                    x.CompanyId == company.Id && x.Status == EntityStatus.Active
                )
                .Select(x => x.IndustryId)
                .ToListAsync(ct);
            if (brand.IndustryIds.Count == 0 && company.IndustryId is { } primary)
                brand.IndustryIds.Add(primary);
            if (brand.IndustryIds.Count > 0)
                brand.Industry = string.Join(
                    ", ",
                    await db
                        .IndustryRecords.Where(i => brand.IndustryIds.Contains(i.Id))
                        .OrderBy(i => i.IndustryName)
                        .Select(i => i.IndustryName)
                        .ToListAsync(ct)
                );
            var team = await db
                .RecruiterProfileRecords.AsNoTracking()
                .Where(p =>
                    p.CompanyId == company.Id
                    && p.Status == EntityStatus.Active
                    && p.Account.Status == EntityStatus.Active
                )
                .OrderBy(p => p.JoinedAt)
                .Select(p => new EmployerCompanyMember(
                    p.Id,
                    p.Account.FullName,
                    p.JobTitle,
                    p.Account.Email,
                    true
                ))
                .ToListAsync(ct);
            var now = DateTimeOffset.UtcNow;
            return new(
                company.Id,
                company.LegalName,
                company.TaxCode,
                company.VerificationStatus,
                company.HeadOfficeProvince?.ProvinceName,
                await CanEdit(profile, ct),
                await db.Jobs.CountAsync(
                    j => j.CompanyId == company.Id && !j.IsClosed && j.ExpiresAt > now,
                    ct
                ),
                await db.FollowedCompanyRecords.CountAsync(
                    f => f.CompanyId == company.Id && f.Status == EntityStatus.Active,
                    ct
                ),
                new()
                {
                    Name = company.Name,
                    Description = company.Description,
                    Address = company.AddressLine ?? "",
                    Website = company.WebsiteUrl ?? "",
                    FoundedYear = company.FoundedYear,
                    SizeMin = company.CompanySizeMin,
                    SizeMax = company.CompanySizeMax,
                    Brand = brand,
                    Revision = Revision(company),
                },
                team
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCompanyController.ToResponse"
            );
            throw;
        }
    }

    private static string Revision(Company company)
    {
        try
        {
            return Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                company.Name,
                                company.Description,
                                company.Culture,
                                company.LogoFileId,
                                company.CoverFileId,
                                company.AddressLine,
                                company.WebsiteUrl,
                                company.FoundedYear,
                                company.CompanySizeMin,
                                company.CompanySizeMax,
                            }
                        )
                    )
                )
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCompanyController.Revision"
            );
            throw;
        }
    }

    internal static EmployerCompanyBrand ReadBrand(Company company) =>
        ReadBrand(company.Culture, company.LogoFile?.Url, company.CoverFile?.Url);

    internal static EmployerCompanyBrand ReadBrand(
        string? culture,
        string? logoUrl,
        string? coverUrl
    )
    {
        try
        {
            EmployerCompanyBrand? brand = null;
            if (culture?.TrimStart().StartsWith('{') == true)
            {
                try
                {
                    brand = JsonSerializer.Deserialize<EmployerCompanyBrand>(culture, JsonOptions);
                }
                catch (JsonException)
                { /* Legacy free-text company culture remains readable. */
                }
            }

            brand ??= new() { Story = culture ?? "" };
            if (string.IsNullOrEmpty(brand.CoverUrl))
                brand.CoverUrl = coverUrl ?? "";
            if (string.IsNullOrEmpty(brand.LogoUrl))
                brand.LogoUrl = logoUrl ?? "";
            return brand;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCompanyController.ReadBrand"
            );
            throw;
        }
    }

    private static void Validate(EmployerCompanyDocument document)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(document.Name))
                throw new ArgumentException("Vui lòng nhập tên hiển thị doanh nghiệp.");
            if (
                document.Description is null
                || document.Address is null
                || document.Website is null
            )
                throw new ArgumentException("Thông tin doanh nghiệp không hợp lệ.");
            if (document.SizeMin > document.SizeMax)
                throw new ArgumentException("Quy mô tối thiểu không được lớn hơn quy mô tối đa.");
            var brand = document.Brand ?? throw new ArgumentException("Thiếu thông tin hồ sơ.");
            if (
                brand.Highlights is null
                || brand.Benefits is null
                || brand.HiringProcess is null
                || brand.Activities is null
                || brand.Gallery is null
            )
                throw new ArgumentException("Danh sách thông tin hồ sơ không hợp lệ.");
            if (
                new[]
                {
                    brand.EnglishName,
                    brand.Representative,
                    brand.Capital,
                    brand.WorkingHours,
                }.Any(x => x is null || x.Length > 300)
                || new[] { brand.Story, brand.Values, brand.Workplace, brand.WorkLife }.Any(x =>
                    x is null || x.Length > 8000
                )
            )
                throw new ArgumentException("Nội dung hồ sơ vượt quá độ dài cho phép.");
            foreach (
                var list in new[]
                {
                    brand.Highlights,
                    brand.Benefits,
                    brand.HiringProcess,
                    brand.Activities,
                }
            )
                if (
                    list.Count > 30
                    || list.Any(x =>
                        x is null
                        || string.IsNullOrWhiteSpace(x.Title)
                        || x.Title.Length > 200
                        || x.Description is null
                        || x.Description.Length > 2000
                        || x.Tag is null
                        || x.Tag.Length > 100
                    )
                )
                    throw new ArgumentException(
                        "Mỗi danh sách tối đa 30 mục; cần tiêu đề và nội dung hợp lệ."
                    );
            if (brand.Gallery.Count > 12)
                throw new ArgumentException("Thư viện tối đa 12 ảnh.");
            foreach (
                var url in brand
                    .Gallery.Where(url =>
                        !System.Text.RegularExpressions.Regex.IsMatch(
                            url ?? "",
                            @"^/uploads/companies/gallery/[a-f0-9]{32}\.(png|jpg)$"
                        )
                    )
                    .Concat([document.Website])
            )
                if (
                    url is null
                    || url.Length > 2048
                    || (
                        url.Length > 0
                        && (
                            !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                            || uri.Scheme is not ("https" or "http")
                        )
                    )
                )
                    throw new ArgumentException(
                        "Liên kết website và hình ảnh phải là URL HTTP/HTTPS hợp lệ."
                    );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerCompanyController.Validate"
            );
            throw;
        }
    }
}
