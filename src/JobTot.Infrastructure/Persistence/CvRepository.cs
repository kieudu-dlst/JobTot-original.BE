using System.Text.Json;
using JobTot.Application.Cvs;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

public sealed class CvRepository(RecruitmentDbContext db) : ICvRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed class TemplateRow
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string Category { get; init; }
        public required string PublicationState { get; init; }
        public Guid Version { get; init; }
        public required string DesignJson { get; init; }
    }

    private IQueryable<TemplateRow> TemplateRows(bool admin) =>
        Templates(admin)
            .AsNoTracking()
            .Select(t => new TemplateRow
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Category = t.Category.Name,
                PublicationState = t.PublicationState,
                Version = t.Version,
                DesignJson = t.DesignJson!,
            });

    private static CvTemplateDto Template(TemplateRow t) =>
        new(
            t.Id,
            t.Name,
            t.Description,
            t.Category,
            t.PublicationState,
            t.Version,
            Document(t.DesignJson)
        );

    private static CvDocument Document(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CvDocument>(json, Json)!;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.Document"
            );
            throw;
        }
    }

    private static CvTemplateDto Template(CvTemplate t)
    {
        try
        {
            return new(
                t.Id,
                t.Name,
                t.Description,
                t.Category.Name,
                t.PublicationState,
                t.Version,
                Document(t.DesignJson!)
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
                "CvRepository.Template"
            );
            throw;
        }
    }

    private static OnlineCvDto Cv(CandidateCv c)
    {
        try
        {
            return new(
                c.Id,
                c.Title,
                c.DocumentVersion,
                c.IsDefault,
                Document(c.OnlineDocumentJson!)
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(exception, "CvRepository.Cv");
            throw;
        }
    }

    private IQueryable<CvTemplate> Templates(bool admin)
    {
        try
        {
            return db.Set<CvTemplate>()
                .Include(t => t.Category)
                .Where(t =>
                    t.Status == EntityStatus.Active
                    && t.DesignJson != null
                    && (admin || t.PublicationState == "published")
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
                "CvRepository.Templates"
            );
            throw;
        }
    }

    private IQueryable<CandidateCv> Owned(Guid accountId)
    {
        try
        {
            return db.CandidateCvRecords.Where(c =>
                c.Candidate.AccountId == accountId
                && c.Candidate.Status == EntityStatus.Active
                && c.Status == EntityStatus.Active
                && c.OnlineDocumentJson != null
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(exception, "CvRepository.Owned");
            throw;
        }
    }

    public async Task<IReadOnlyList<CvTemplateDto>> TemplatesAsync(bool admin, CancellationToken ct)
    {
        try
        {
            return (await TemplateRows(admin).OrderBy(t => t.Name).ToListAsync(ct))
                .Select(Template)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.TemplatesAsync"
            );
            throw;
        }
    }

    public async Task<CvTemplateDto?> TemplateAsync(Guid id, bool admin, CancellationToken ct)
    {
        try
        {
            return await TemplateRows(admin).SingleOrDefaultAsync(t => t.Id == id, ct) is { } t
                ? Template(t)
                : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.TemplateAsync"
            );
            throw;
        }
    }

    public async Task<CvTemplateDto?> WriteTemplateAsync(
        Guid? id,
        Guid accountId,
        CvTemplateWrite input,
        CancellationToken ct
    )
    {
        try
        {
            var template = id is null
                ? new CvTemplate { CreatedAccountId = accountId }
                : await Templates(true).SingleOrDefaultAsync(t => t.Id == id, ct);
            if (template is null)
                return null;
            if (id is not null && template.Version != input.Version)
                throw new CvVersionConflictException();
            var category = await db.CvCategoryRecords.FirstOrDefaultAsync(
                c => c.Name == input.Category && c.Status == EntityStatus.Active,
                ct
            );
            if (category is null)
            {
                category = new CvCategory { Name = input.Category, CreatedAccountId = accountId };
                db.Add(category);
            }

            template.Category = category;
            template.Name = input.Name.Trim();
            template.Description = input.Description;
            template.PublicationState = input.PublicationState;
            template.DesignJson = JsonSerializer.Serialize(input.Document, Json);
            template.Version = Guid.NewGuid();
            template.UpdatedDate = DateTimeOffset.UtcNow;
            template.UpdatedAccountId = accountId;
            if (id is null)
                db.Add(template);
            await Persist(ct);
            return Template(template);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.WriteTemplateAsync"
            );
            throw;
        }
    }

    public async Task<IReadOnlyList<OnlineCvListDto>> ListAsync(
        Guid accountId,
        CancellationToken ct
    )
    {
        try
        {
            return await Owned(accountId)
                .AsNoTracking()
                .OrderByDescending(c => c.UpdatedDate ?? c.CreatedDate)
                .Select(c => new OnlineCvListDto(
                    c.Id,
                    c.Title,
                    c.DocumentVersion,
                    c.IsDefault,
                    c.UpdatedDate ?? c.CreatedDate
                ))
                .ToListAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.ListAsync"
            );
            throw;
        }
    }

    public async Task<OnlineCvDto?> CreateAsync(
        Guid accountId,
        Guid templateId,
        CancellationToken ct
    )
    {
        try
        {
            var template = await Templates(false)
                .AsNoTracking()
                .Where(t => t.Id == templateId)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.DesignJson,
                })
                .SingleOrDefaultAsync(ct);
            var candidate = await db
                .CandidateProfileRecords.AsNoTracking()
                .Where(c => c.AccountId == accountId && c.Status == EntityStatus.Active)
                .Select(c => new
                {
                    c.Id,
                    c.Account.FullName,
                    c.Account.Email,
                    c.Account.Phone,
                })
                .SingleOrDefaultAsync(ct);
            if (template is null || candidate is null)
                return null;
            var document = Document(template.DesignJson!);
            document.FullName = candidate.FullName;
            document.Email = candidate.Email ?? "";
            document.Phone = candidate.Phone ?? "";
            var cv = new CandidateCv
            {
                CandidateId = candidate.Id,
                CvTemplateId = template.Id,
                Title = template.Name,
                OnlineDocumentJson = JsonSerializer.Serialize(document, Json),
                CreatedAccountId = accountId,
            };
            db.Add(cv);
            await db.SaveChangesAsync(ct);
            return Cv(cv);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.CreateAsync"
            );
            throw;
        }
    }

    public async Task<OnlineCvDto?> GetAsync(Guid accountId, Guid id, CancellationToken ct)
    {
        try
        {
            var cv = await Owned(accountId)
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.DocumentVersion,
                    c.IsDefault,
                    c.OnlineDocumentJson,
                })
                .SingleOrDefaultAsync(ct);
            return cv is null
                ? null
                : new(
                    cv.Id,
                    cv.Title,
                    cv.DocumentVersion,
                    cv.IsDefault,
                    Document(cv.OnlineDocumentJson!)
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
                "CvRepository.GetAsync"
            );
            throw;
        }
    }

    public async Task<OnlineCvDto?> SaveAsync(
        Guid accountId,
        Guid id,
        CvWrite input,
        CancellationToken ct
    )
    {
        try
        {
            var cv = await Owned(accountId).SingleOrDefaultAsync(c => c.Id == id, ct);
            if (cv is null)
                return null;
            if (cv.DocumentVersion != input.Version)
                throw new CvVersionConflictException();
            cv.Title = input.Title.Trim();
            cv.OnlineDocumentJson = JsonSerializer.Serialize(input.Document, Json);
            cv.DocumentVersion = Guid.NewGuid();
            cv.UpdatedDate = DateTimeOffset.UtcNow;
            cv.UpdatedAccountId = accountId;
            await Persist(ct);
            return Cv(cv);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.SaveAsync"
            );
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid accountId, Guid id, Guid version, CancellationToken ct)
    {
        try
        {
            var cv = await Owned(accountId).SingleOrDefaultAsync(c => c.Id == id, ct);
            if (cv is null)
                return false;
            if (cv.DocumentVersion != version)
                throw new CvVersionConflictException();
            cv.Status = EntityStatus.Deleted;
            cv.DocumentVersion = Guid.NewGuid();
            cv.IsDefault = false;
            await Persist(ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.DeleteAsync"
            );
            throw;
        }
    }

    private async Task Persist(CancellationToken ct)
    {
        try
        {
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new CvVersionConflictException();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvRepository.Persist"
            );
            throw;
        }
    }
}
