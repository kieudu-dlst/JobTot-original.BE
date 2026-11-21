using JobTot.Application.Profiles;

namespace JobTot.Application.Features;

public sealed class CandidateProfileService(
    ICandidateProfileRepository repository,
    IAvatarStorage files,
    IDataFailureClassifier failures,
    ICurrentActor actor
) : ApplicationService(actor)
{
    private Guid AccountId => Actor.AccountId;
    public async Task<UseCaseResult> SetDefault(Guid id, CvDefaultInput input, CancellationToken ct)
    {
        try
        {
            return await repository.SetCvDefaultAsync(AccountId, id, input.IsDefault, ct) is { } cvs
                ? Success(cvs)
                : NotFound();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileController.SetDefault"
            );
            throw;
        }
    }

    public sealed record CvDefaultInput(bool IsDefault);
    public async Task<UseCaseResult> DeleteCv(
        Guid id,
        CancellationToken ct
    )
    {
        try
        {
            return await repository.DeleteCvAsync(AccountId, id, ct) is { } cvs
                ? Success(cvs)
                : NotFound();
        }
        catch (Exception failure) when (failures.IsConcurrency(failure))
        {
            return Conflict(
                new FailureDetails
                {
                    Detail = "Danh sách CV đã thay đổi. Vui lòng tải lại hồ sơ và thử lại.",
                }
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
                "CandidateProfileController.DeleteCv"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Get(CancellationToken ct)
    {
        try
        {
            return await repository.GetAsync(AccountId, ct) is { } p ? Success(p) : NotFound();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileController.Get"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Put(
        CandidateProfileUpdateRequest request,
        CancellationToken ct
    )
    {
        try
        {
            return await repository.SaveAsync(AccountId, request.Profile, ct, request.Phone) is { } p
                ? Success(p)
                : NotFound();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileController.Put"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Upload(UploadFile file, CancellationToken ct)
    {
        try
        {
            if (
                file.Length is <= 0 or > 5 * 1024 * 1024
                || !string.Equals(
                    Path.GetExtension(file.FileName),
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return Invalid(new FailureDetails { Detail = "Chỉ chấp nhận PDF tối đa 5 MB." });
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            var content = buffer.ToArray();
            if (
                content.Length < 5
                || System.Text.Encoding.ASCII.GetString(content, 0, 5) != "%PDF-"
            )
                return Invalid(new FailureDetails { Detail = "Nội dung tệp PDF không hợp lệ." });
            var name = Path.GetFileName(file.FileName);
            if (name.Length > 200)
                return Invalid(new FailureDetails { Detail = "Tên tệp quá dài." });
            return await repository.UploadCvAsync(AccountId, name, content, ct) is { } cv
                ? Success(cv)
                : NotFound();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileController.Upload"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Download(Guid id, CancellationToken ct)
    {
        try
        {
            return await repository.GetCvAsync(AccountId, id, ct) is { } cv
                ? UseCaseResult.Download(cv.Content, "application/pdf", cv.Name)
                : NotFound();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileController.Download"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> Avatar(
UploadFile file,
        CancellationToken ct
    )
    {
        try
        {
            if (file.Length is <= 0 or > 2 * 1024 * 1024)
                return Invalid(
                    new FailureDetails { Detail = "Chọn ảnh PNG hoặc JPG tối đa 2 MB." }
                );
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            var bytes = buffer.ToArray();
            var png =
                bytes.Length >= 24
                && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8);
            var jpeg =
                bytes.Length >= 4
                && bytes[0] == 255
                && bytes[1] == 216
                && bytes[2] == 255
                && bytes[^2] == 255
                && bytes[^1] == 217;
            if (!png && !jpeg)
                return Invalid(
                    new FailureDetails { Detail = "Nội dung ảnh PNG hoặc JPG không hợp lệ." }
                );
            var previous = await repository.GetAsync(AccountId, ct);
            if (previous is null)
                return NotFound();
            var storage = files;
            var avatar = await storage.SaveAsync(bytes, png, ct);
            // Do not delete the new file on a database cancellation: commit may have completed.
            var profile = await repository.SetAvatarAsync(AccountId, avatar, ct);
            if (profile is null)
            {
                storage.Delete(avatar);
                return NotFound();
            }
            storage.Delete(previous.Profile.AvatarUrl);
            return Success(profile);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileController.Avatar"
            );
            throw;
        }
    }
    public async Task<UseCaseResult> RemoveAvatar(CancellationToken ct)
    {
        try
        {
            var previous = await repository.GetAsync(AccountId, ct);
            if (previous is null)
                return NotFound();
            var profile = await repository.SetAvatarAsync(AccountId, null, ct);
            if (profile is null)
                return NotFound();
            files.Delete(previous.Profile.AvatarUrl);
            return Success(profile);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateProfileController.RemoveAvatar"
            );
            throw;
        }
    }
}
