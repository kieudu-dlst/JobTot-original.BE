using System.Security.Claims;
using JobTot.Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/candidate/profile")]
[Authorize(Policy = CandidateAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CandidateProfileController(
    ICandidateProfileRepository repository,
    AvatarFileStorage files
) : ControllerBase
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPut("cvs/{id:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid id, CvDefaultInput input, CancellationToken ct)
    {
        try
        {
            return await repository.SetCvDefaultAsync(AccountId, id, input.IsDefault, ct) is { } cvs
                ? Ok(cvs)
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

    [HttpGet]
    public async Task<ActionResult<CandidateProfileResponse>> Get(CancellationToken ct)
    {
        try
        {
            return await repository.GetAsync(AccountId, ct) is { } p ? Ok(p) : NotFound();
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

    [HttpPut]
    public async Task<ActionResult<CandidateProfileResponse>> Put(
        CandidateProfileDocument document,
        CancellationToken ct
    )
    {
        try
        {
            return await repository.SaveAsync(AccountId, document, ct) is { } p
                ? Ok(p)
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

    [HttpPost("cvs")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ProfileCv>> Upload(IFormFile file, CancellationToken ct)
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
                return BadRequest(new ProblemDetails { Detail = "Chỉ chấp nhận PDF tối đa 5 MB." });
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            var content = buffer.ToArray();
            if (
                content.Length < 5
                || System.Text.Encoding.ASCII.GetString(content, 0, 5) != "%PDF-"
            )
                return BadRequest(new ProblemDetails { Detail = "Nội dung tệp PDF không hợp lệ." });
            var name = Path.GetFileName(file.FileName);
            if (name.Length > 200)
                return BadRequest(new ProblemDetails { Detail = "Tên tệp quá dài." });
            return await repository.UploadCvAsync(AccountId, name, content, ct) is { } cv
                ? Ok(cv)
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

    [HttpGet("cvs/{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        try
        {
            return await repository.GetCvAsync(AccountId, id, ct) is { } cv
                ? File(cv.Content, "application/pdf", cv.Name)
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

    [HttpPost("avatar")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<CandidateProfileResponse>> Avatar(
        IFormFile file,
        CancellationToken ct
    )
    {
        try
        {
            if (file.Length is <= 0 or > 2 * 1024 * 1024)
                return BadRequest(
                    new ProblemDetails { Detail = "Chọn ảnh PNG hoặc JPG tối đa 2 MB." }
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
                return BadRequest(
                    new ProblemDetails { Detail = "Nội dung ảnh PNG hoặc JPG không hợp lệ." }
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
            return Ok(profile);
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

    [HttpDelete("avatar")]
    public async Task<ActionResult<CandidateProfileResponse>> RemoveAvatar(CancellationToken ct)
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
            return Ok(profile);
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
