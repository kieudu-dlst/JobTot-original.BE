using System.Text.Json;
using JobTot.Application.Profiles;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

public sealed class CandidateProfileRepository(RecruitmentDbContext db)
    : ICandidateProfileRepository
{
    private sealed record CvState(
        Guid Id,
        string Title,
        bool IsDefault,
        bool IsOnline,
        Guid Version
    );

    private Task<List<ProfileCv>> CvSummariesAsync(Guid candidateId, CancellationToken ct) =>
        db
            .CandidateCvRecords.AsNoTracking()
            .Where(c => c.CandidateId == candidateId && c.Status == EntityStatus.Active)
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new ProfileCv(c.Id, c.Title, c.IsDefault, c.OnlineDocumentJson != null))
            .ToListAsync(ct);

    private void SetDefault(CvState state, bool value)
    {
        // Update only the flag; never read or rewrite the uploaded file/document payload.
        var cv = db.CandidateCvRecords.Local.FirstOrDefault(c => c.Id == state.Id);
        if (cv is null)
        {
            cv = new CandidateCv
            {
                Id = state.Id,
                DocumentVersion = state.Version,
                IsDefault = state.IsDefault,
            };
            db.CandidateCvRecords.Attach(cv);
        }
        var flag = db.Entry(cv).Property(c => c.IsDefault);
        flag.OriginalValue = state.IsDefault;
        flag.CurrentValue = value;
        flag.IsModified = state.IsDefault != value;
    }

    public async Task<IReadOnlyList<ProfileCv>?> SetCvDefaultAsync(
        Guid accountId,
        Guid cvId,
        bool isDefault,
        CancellationToken ct
    )
    {
        try
        {
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(
                            System.Data.IsolationLevel.Serializable,
                            ct
                        )
                        : null;
                    var cvs = await db
                        .CandidateCvRecords.AsNoTracking()
                        .Where(c =>
                            c.Candidate.AccountId == accountId
                            && c.Candidate.Status == EntityStatus.Active
                            && c.Candidate.Account.Status == EntityStatus.Active
                            && c.Status == EntityStatus.Active
                        )
                        .Select(c => new CvState(
                            c.Id,
                            c.Title,
                            c.IsDefault,
                            c.OnlineDocumentJson != null,
                            c.DocumentVersion
                        ))
                        .ToListAsync(ct);
                    var selected = cvs.SingleOrDefault(c => c.Id == cvId);
                    if (selected is null)
                        return (IReadOnlyList<ProfileCv>?)null;
                    if (isDefault)
                    {
                        foreach (var cv in cvs.Where(c => c.Id != cvId && c.IsDefault))
                            SetDefault(cv, false);
                        // Clear the old primary first to respect the filtered unique index.
                        await db.SaveChangesAsync(ct);
                    }

                    SetDefault(selected, isDefault);
                    await db.SaveChangesAsync(ct);
                    if (transaction is not null)
                        await transaction.CommitAsync(ct);
                    return (IReadOnlyList<ProfileCv>)
                        cvs.Select(c => new ProfileCv(
                                c.Id,
                                c.Title,
                                c.Id == cvId ? isDefault : !isDefault && c.IsDefault,
                                c.IsOnline
                            ))
                            .ToList();
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileRepository.SetCvDefaultAsync"
            );
            throw;
        }
    }

    private async Task<CandidateProfile?> Find(Guid id, CancellationToken ct, bool tracking = true)
    {
        try
        {
            var query = db
                .CandidateProfileRecords.Include(x => x.Account)
                .Where(x =>
                    x.AccountId == id
                    && x.Status == EntityStatus.Active
                    && x.Account.Status == EntityStatus.Active
                );
            return await (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileRepository.Find"
            );
            throw;
        }
    }

    private static CandidateProfileResponse Response(
        CandidateProfile p,
        IReadOnlyList<ProfileCv> cvs
    )
    {
        try
        {
            var document = p.DetailsJson is null
                ? new CandidateProfileDocument
                {
                    FullName = p.Account.FullName,
                    Headline = p.Headline ?? "",
                    Summary = p.Summary ?? "",
                    BirthDate = p.BirthDate,
                    ReadyStatus = p.ReadyStatus ?? "looking",
                    ExpectedSalaryMin = p.ExpectedSalaryMin,
                    ExpectedSalaryMax = p.ExpectedSalaryMax,
                }
                : JsonSerializer.Deserialize<CandidateProfileDocument>(p.DetailsJson)!;
            return new(document, p.Account.Email, p.Account.Phone, cvs);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileRepository.Response"
            );
            throw;
        }
    }

    public async Task<CandidateProfileResponse?> GetSummaryAsync(
        Guid accountId,
        CancellationToken ct
    )
    {
        try
        {
            var profile = await Find(accountId, ct, tracking: false);
            return profile is null
                ? null
                : Response(profile, await CvSummariesAsync(profile.Id, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileRepository.GetSummaryAsync"
            );
            throw;
        }
    }

    public async Task<CandidateProfileResponse?> GetAsync(Guid accountId, CancellationToken ct)
    {
        try
        {
            var p = await Find(accountId, ct, tracking: false);
            if (p is null)
                return null;
            var since = DateTimeOffset.UtcNow.AddDays(-7);
            var reviewed = db
                .CandidateJobSubmissions.AsNoTracking()
                .Where(s =>
                    s.CandidateId == p.Id
                    && s.ReviewedAt >= since
                    && s.Job.Company.Status == EntityStatus.Active
                );
            var views = await reviewed.CountAsync(ct);
            var visitors = await reviewed
                .GroupBy(s => new { s.Job.CompanyId, s.Job.Company.Name })
                .Select(group => new
                {
                    group.Key.CompanyId,
                    group.Key.Name,
                    ViewedAt = group.Max(s => s.ReviewedAt!.Value),
                })
                .OrderByDescending(v => v.ViewedAt)
                .ThenBy(v => v.CompanyId)
                .Take(5)
                .Select(v => new CandidateProfileVisitor(v.CompanyId, v.Name, v.ViewedAt))
                .ToListAsync(ct);
            var invitations = await db
                .InterviewRecords.AsNoTracking()
                .CountAsync(
                    i =>
                        i.Application.CandidateId == p.Id
                        && i.CreatedDate >= since
                        && i.Status == EntityStatus.Active
                        && i.Application.Status == EntityStatus.Active
                        && i.Application.Campaign.Status == EntityStatus.Active
                        && i.Application.Campaign.Company.Status == EntityStatus.Active
                        && i.InterviewStatus != "cancelled"
                        && i.InterviewStatus != "canceled"
                        && i.InterviewStatus != "declined",
                    ct
                );
            return Response(p, await CvSummariesAsync(p.Id, ct)) with
            {
                Activity = new(views, invitations, visitors),
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileRepository.GetAsync"
            );
            throw;
        }
    }

    public async Task<CandidateProfileResponse?> SaveAsync(
        Guid accountId,
        CandidateProfileDocument document,
        CancellationToken ct
    )
    {
        try
        {
            var p = await Find(accountId, ct);
            if (p is null)
                return null;
            var previous = Response(p, []).Profile;
            document.AvatarUrl = previous.AvatarUrl;
            if (document.ProvinceId is { } addressProvinceId)
            {
                var addressProvince =
                    await db
                        .ProvinceRecords.AsNoTracking()
                        .SingleOrDefaultAsync(
                            x => x.Id == addressProvinceId && x.Status == EntityStatus.Active,
                            ct
                        )
                    ?? throw new ArgumentException(
                        "Tỉnh/thành của địa chỉ không tồn tại hoặc đã ngừng hoạt động."
                    );
                document.Location = addressProvince.ProvinceName;
                p.ProvinceId = addressProvince.Id;
            }
            else if (
                !string.IsNullOrEmpty(document.Location)
                && document.Location != previous.Location
            )
            {
                var addressProvince =
                    await db
                        .ProvinceRecords.AsNoTracking()
                        .SingleOrDefaultAsync(
                            x =>
                                x.ProvinceName == document.Location
                                && x.Status == EntityStatus.Active,
                            ct
                        )
                    ?? throw new ArgumentException(
                        "Vui lòng chọn địa chỉ từ danh sách tỉnh/thành."
                    );
                document.ProvinceId = addressProvince.Id;
                p.ProvinceId = addressProvince.Id;
            }

            var workTypes = new[]
            {
                "Toàn thời gian",
                "Bán thời gian",
                "Làm việc từ xa",
                "Kết hợp (Hybrid)",
            };
            if (
                !string.IsNullOrEmpty(document.WorkType)
                && !workTypes.Contains(document.WorkType)
                && document.WorkType != previous.WorkType
            )
                throw new ArgumentException("Vui lòng chọn hình thức làm việc có sẵn.");
            if (document.DesiredProvinceId is { } provinceId)
            {
                var province =
                    await db
                        .ProvinceRecords.AsNoTracking()
                        .SingleOrDefaultAsync(
                            x => x.Id == provinceId && x.Status == EntityStatus.Active,
                            ct
                        )
                    ?? throw new ArgumentException(
                        "Địa điểm ưu tiên không tồn tại hoặc đã ngừng hoạt động."
                    );
                document.DesiredLocation = province.ProvinceName;
            }
            else if (
                !string.IsNullOrEmpty(document.DesiredLocation)
                && document.DesiredLocation != previous.DesiredLocation
            )
            {
                var province =
                    await db
                        .ProvinceRecords.AsNoTracking()
                        .SingleOrDefaultAsync(
                            x =>
                                x.ProvinceName == document.DesiredLocation
                                && x.Status == EntityStatus.Active,
                            ct
                        )
                    ?? throw new ArgumentException(
                        "Vui lòng chọn địa điểm ưu tiên từ danh sách tỉnh/thành."
                    );
                document.DesiredProvinceId = province.Id;
            }

            p.DetailsJson = JsonSerializer.Serialize(document);
            p.Account.FullName = document.FullName.Trim();
            p.Headline = document.Headline;
            p.Summary = document.Summary;
            p.BirthDate = document.BirthDate;
            p.ReadyStatus = document.ReadyStatus;
            p.ExpectedSalaryMin = document.ExpectedSalaryMin;
            p.ExpectedSalaryMax = document.ExpectedSalaryMax;
            p.UpdatedDate = DateTimeOffset.UtcNow;
            p.UpdatedAccountId = accountId;
            await db.SaveChangesAsync(ct);
            return Response(p, await CvSummariesAsync(p.Id, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileRepository.SaveAsync"
            );
            throw;
        }
    }

    public async Task<CandidateProfileResponse?> SetAvatarAsync(
        Guid accountId,
        string? avatarUrl,
        CancellationToken ct
    )
    {
        try
        {
            var p = await Find(accountId, ct);
            if (p is null)
                return null;
            var document = Response(p, []).Profile;
            document.AvatarUrl = avatarUrl;
            p.DetailsJson = JsonSerializer.Serialize(document);
            p.UpdatedDate = DateTimeOffset.UtcNow;
            p.UpdatedAccountId = accountId;
            await db.SaveChangesAsync(ct);
            return Response(p, await CvSummariesAsync(p.Id, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileRepository.SetAvatarAsync"
            );
            throw;
        }
    }

    public async Task<ProfileCv?> UploadCvAsync(
        Guid accountId,
        string name,
        byte[] content,
        CancellationToken ct
    )
    {
        try
        {
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(
                            System.Data.IsolationLevel.Serializable,
                            ct
                        )
                        : null;
                    var candidateId = await db
                        .CandidateProfileRecords.AsNoTracking()
                        .Where(p =>
                            p.AccountId == accountId
                            && p.Status == EntityStatus.Active
                            && p.Account.Status == EntityStatus.Active
                        )
                        .Select(p => (Guid?)p.Id)
                        .SingleOrDefaultAsync(ct);
                    if (candidateId is null)
                        return (ProfileCv?)null;

                    var defaults = await db
                        .CandidateCvRecords.AsNoTracking()
                        .Where(c => c.CandidateId == candidateId.Value && c.IsDefault)
                        .Select(c => new CvState(
                            c.Id,
                            c.Title,
                            c.IsDefault,
                            c.OnlineDocumentJson != null,
                            c.DocumentVersion
                        ))
                        .ToListAsync(ct);
                    foreach (var previous in defaults)
                        SetDefault(previous, false);
                    // Release the unique default slot before inserting the new file.
                    if (defaults.Count > 0)
                        await db.SaveChangesAsync(ct);

                    var cv = new CandidateCv
                    {
                        CandidateId = candidateId.Value,
                        Title = name,
                        ContentJson = JsonSerializer.Serialize(Convert.ToBase64String(content)),
                        IsDefault = true,
                        CreatedAccountId = accountId,
                    };
                    db.CandidateCvRecords.Add(cv);
                    await db.SaveChangesAsync(ct);
                    if (transaction is not null)
                        await transaction.CommitAsync(ct);
                    return new ProfileCv(cv.Id, cv.Title, cv.IsDefault);
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileRepository.UploadCvAsync"
            );
            throw;
        }
    }

    public async Task<(string Name, byte[] Content)?> GetCvAsync(
        Guid accountId,
        Guid cvId,
        CancellationToken ct
    )
    {
        try
        {
            var cv = await db
                .CandidateCvRecords.AsNoTracking()
                .Where(x =>
                    x.Id == cvId
                    && x.Candidate.AccountId == accountId
                    && x.Status == EntityStatus.Active
                )
                .Select(x => new { x.Title, x.ContentJson })
                .SingleOrDefaultAsync(ct);
            if (cv?.ContentJson is null)
                return null;
            return (
                cv.Title,
                Convert.FromBase64String(JsonSerializer.Deserialize<string>(cv.ContentJson)!)
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
                "CandidateProfileRepository.GetCvAsync"
            );
            throw;
        }
    }
}
