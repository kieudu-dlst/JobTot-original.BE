using System.ComponentModel.DataAnnotations;
using JobTot.Domain.Entities;

namespace JobTot.Application.Features;

public sealed class CareerArticlesService(
    IRecruitmentData db,
    IArticleImageStorage images,
    ICurrentActor actor
) : ApplicationService(actor)
{
    public Task<UseCaseResult> Published(CancellationToken ct) =>
        ReadWithCancellationAsync(
            async () =>
                Success(
                    await db
                        .CareerArticles.ReadOnly()
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
    public async Task<UseCaseResult> UploadCover(
        Guid id,
UploadFile file,
        Guid version,
        CancellationToken ct
    )
    {
        var article = await db.CareerArticles.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (article is null)
            return NotFound();
        if (article.Version != version)
            return Conflict(
                new
                {
                    detail = "Bài viết đã thay đổi. Hãy tải lại trước khi tải ảnh."
                }
            );
        if (file.Length is <= 0 or > 5 * 1024 * 1024)
            return Invalid(new
            {
                detail = "Ảnh bìa tối đa 5 MB."
            });
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        var bytes = stream.ToArray();
        var png =
            bytes.Length >= 8
            && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
        if (!png && !jpeg)
            return Invalid(new
            {
                detail = "Chọn ảnh PNG hoặc JPEG hợp lệ."
            });
        var url = await images.SaveAsync(
bytes, png, ct);
        article.CoverUrl = url;
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
        catch (DataConcurrencyException)
        {
            images.Delete(url);
            return Conflict(
                new
                {
                    detail = "Bài viết đã thay đổi. Hãy tải lại trước khi tải ảnh."
                }
            );
        }
        catch
        {
            images.Delete(url);
            throw;
        }
        return Success(article);
    }
    public Task<UseCaseResult> Read(Guid id, CancellationToken ct) =>
        ReadWithCancellationAsync(async () =>
        {
            var article = await db
                .CareerArticles.ReadOnly()
                .SingleOrDefaultAsync(x => x.Id == id && x.Published, ct);
            return article is null ? NotFound() : Success(article);
        });
    public Task<UseCaseResult> List(CancellationToken ct) =>
        ReadWithCancellationAsync(
            async () =>
                Success(
                    await db
                        .CareerArticles.ReadOnly()
                        .OrderByDescending(x => x.UpdatedAt)
                        .ThenBy(x => x.Id)
                        .Take(200)
                        .ToListAsync(ct)
                )
        );

    private async Task<UseCaseResult> ReadWithCancellationAsync(Func<Task<UseCaseResult>> query)
    {
        // Do not open a database connection for a client that has already disconnected.
        if (Actor.Cancellation.IsCancellationRequested)
            return Cancelled();
        try
        {
            return await query();
        }
        catch (Exception exception)
            when (CancellationPolicy.IsCancellation(exception, Actor.Cancellation))
        {
            return Cancelled();
        }
    }
    public Task<UseCaseResult> Create(ArticleInput input, CancellationToken ct) =>
        Save(null, input, ct);
    public Task<UseCaseResult> Update(Guid id, ArticleInput input, CancellationToken ct) =>
        Save(id, input, ct);

    private async Task<UseCaseResult> Save(Guid? id, ArticleInput input, CancellationToken ct)
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
            return Invalid(
                new
                {
                    detail = "Nhập tiêu đề, danh mục; bài xuất bản cần có mô tả và nội dung."
                }
            );
        var article = id.HasValue
            ? await db.CareerArticles.SingleOrDefaultAsync(x => x.Id == id.Value, ct)
            : new CareerArticle { Id = Guid.NewGuid() };
        if (article is null)
            return NotFound();
        if (id.HasValue && article.Version != input.Version)
            return Conflict(
                new
                {
                    detail = "Bài viết đã thay đổi ở nơi khác. Hãy tải lại trước khi lưu."
                }
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
        catch (DataConcurrencyException)
        {
            return Conflict(
                new
                {
                    detail = "Bài viết đã thay đổi ở nơi khác. Hãy tải lại trước khi lưu."
                }
            );
        }
        return Success(article);
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
