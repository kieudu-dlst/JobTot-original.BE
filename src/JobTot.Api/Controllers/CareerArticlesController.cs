using System.ComponentModel.DataAnnotations;
using JobTot.Domain.Entities;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api.Controllers;

[ApiController]
public sealed class CareerArticlesController(
    RecruitmentDbContext db,
    IWebHostEnvironment environment
) : ControllerBase
{
    [HttpGet("api/career-articles")]
    public Task<IActionResult> Published(CancellationToken ct) =>
        ReadWithCancellationAsync(
            async () =>
                Ok(
                    await db
                        .CareerArticles.AsNoTracking()
                        .Where(x => x.Published)
                        .OrderByDescending(x => x.UpdatedAt)
                        .ThenBy(x => x.Id)
                        .Select(x => new
                        {
                            x.Id,
                            x.Title,
                            x.Category,
                            x.Summary,
                            x.UpdatedAt,
                            x.CoverUrl,
                        })
                        .Take(200)
                        .ToListAsync(ct)
                )
        );

    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpPost("api/admin/career-articles/{id:guid}/cover")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadCover(
        Guid id,
        [FromForm] IFormFile file,
        [FromForm] Guid version,
        CancellationToken ct
    )
    {
        var article = await db.CareerArticles.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (article is null)
            return NotFound();
        if (article.Version != version)
            return Conflict(
                new { detail = "Bài viết đã thay đổi. Hãy tải lại trước khi tải ảnh." }
            );
        if (file.Length is <= 0 or > 5 * 1024 * 1024)
            return BadRequest(new { detail = "Ảnh bìa tối đa 5 MB." });
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        var bytes = stream.ToArray();
        var png =
            bytes.Length >= 8
            && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
        if (!png && !jpeg)
            return BadRequest(new { detail = "Chọn ảnh PNG hoặc JPEG hợp lệ." });
        var folder = Path.Combine(
            environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
            "uploads",
            "articles"
        );
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}.{(png ? "png" : "jpg")}";
        var path = Path.Combine(folder, name);
        await System.IO.File.WriteAllBytesAsync(path, bytes, ct);
        article.CoverUrl = "/uploads/articles/" + name;
        article.CoverFileName = Path.GetFileName(file.FileName)[
            ..Math.Min(Path.GetFileName(file.FileName).Length, 255)
        ];
        article.CoverContentType = png ? "image/png" : "image/jpeg";
        article.CoverSize = file.Length;
        article.Version = Guid.NewGuid();
        article.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            System.IO.File.Delete(path);
            return Conflict(
                new { detail = "Bài viết đã thay đổi. Hãy tải lại trước khi tải ảnh." }
            );
        }
        catch
        {
            System.IO.File.Delete(path);
            throw;
        }
        return Ok(article);
    }

    [HttpGet("api/career-articles/{id:guid}")]
    public Task<IActionResult> Read(Guid id, CancellationToken ct) =>
        ReadWithCancellationAsync(async () =>
        {
            var article = await db
                .CareerArticles.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.Published, ct);
            return article is null ? NotFound() : Ok(article);
        });

    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpGet("api/admin/career-articles")]
    public Task<IActionResult> List(CancellationToken ct) =>
        ReadWithCancellationAsync(
            async () =>
                Ok(
                    await db
                        .CareerArticles.AsNoTracking()
                        .OrderByDescending(x => x.UpdatedAt)
                        .ThenBy(x => x.Id)
                        .Take(200)
                        .ToListAsync(ct)
                )
        );

    private async Task<IActionResult> ReadWithCancellationAsync(Func<Task<IActionResult>> query)
    {
        // Do not open a database connection for a client that has already disconnected.
        if (HttpContext.RequestAborted.IsCancellationRequested)
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        try
        {
            return await query();
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, HttpContext.RequestAborted))
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }

    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpPost("api/admin/career-articles")]
    public Task<IActionResult> Create(ArticleInput input, CancellationToken ct) =>
        Save(null, input, ct);

    [Authorize(Policy = AdminAuthentication.Policy)]
    [HttpPut("api/admin/career-articles/{id:guid}")]
    public Task<IActionResult> Update(Guid id, ArticleInput input, CancellationToken ct) =>
        Save(id, input, ct);

    private async Task<IActionResult> Save(Guid? id, ArticleInput input, CancellationToken ct)
    {
        if (
            string.IsNullOrWhiteSpace(input.Title)
            || string.IsNullOrWhiteSpace(input.Category)
            || (
                input.Published
                && (
                    string.IsNullOrWhiteSpace(input.Content)
                    || string.IsNullOrWhiteSpace(input.Summary)
                )
            )
        )
            return BadRequest(
                new { detail = "Nhập tiêu đề, danh mục; bài xuất bản cần có mô tả và nội dung." }
            );
        var article = id.HasValue
            ? await db.CareerArticles.SingleOrDefaultAsync(x => x.Id == id.Value, ct)
            : new CareerArticle { Id = Guid.NewGuid() };
        if (article is null)
            return NotFound();
        if (id.HasValue && article.Version != input.Version)
            return Conflict(
                new { detail = "Bài viết đã thay đổi ở nơi khác. Hãy tải lại trước khi lưu." }
            );
        if (!id.HasValue)
            db.CareerArticles.Add(article);
        article.Title = input.Title.Trim();
        article.Category = input.Category.Trim();
        article.Summary = input.Summary.Trim();
        article.Content = input.Content;
        article.Published = input.Published;
        article.Version = Guid.NewGuid();
        article.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(
                new { detail = "Bài viết đã thay đổi ở nơi khác. Hãy tải lại trước khi lưu." }
            );
        }
        return Ok(article);
    }

    public sealed record ArticleInput(
        [Required, StringLength(200)] string Title,
        [Required, StringLength(100)] string Category,
        [Required(AllowEmptyStrings = true), StringLength(600)] string Summary,
        [Required(AllowEmptyStrings = true), StringLength(20000)] string Content,
        bool Published,
        Guid? Version
    );
}
