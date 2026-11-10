using System.Security.Claims;
using JobTot.Application.Cvs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[
    ApiController,
    Route("api/candidate/online-cvs"),
    Authorize(Policy = CandidateAuthentication.Policy)
]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OnlineCvsController(ICvRepository repository) : ControllerBase
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        try
        {
            return Ok(await repository.ListAsync(AccountId, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "OnlineCvsController.List"
            );
            throw;
        }
    }

    [HttpPost("from-template/{templateId:guid}")]
    public async Task<IActionResult> Create(Guid templateId, CancellationToken ct)
    {
        try
        {
            return await repository.CreateAsync(AccountId, templateId, ct) is { } cv
                ? Created($"/api/candidate/online-cvs/{cv.Id}", cv)
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
                "OnlineCvsController.Create"
            );
            throw;
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        try
        {
            return await repository.GetAsync(AccountId, id, ct) is { } cv ? Ok(cv) : NotFound();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "OnlineCvsController.Get"
            );
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Save(Guid id, CvWrite input, CancellationToken ct)
    {
        try
        {
            try
            {
                return await repository.SaveAsync(AccountId, id, input, ct) is { } cv
                    ? Ok(cv)
                    : NotFound();
            }
            catch (CvVersionConflictException)
            {
                return Conflict(
                    new ProblemDetails
                    {
                        Detail =
                            "CV đã được chỉnh sửa ở tab khác. Hãy tải lại bản mới nhất trước khi tiếp tục.",
                    }
                );
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
                "OnlineCvsController.Save"
            );
            throw;
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid version, CancellationToken ct)
    {
        try
        {
            try
            {
                return await repository.DeleteAsync(AccountId, id, version, ct)
                    ? NoContent()
                    : NotFound();
            }
            catch (CvVersionConflictException)
            {
                return Conflict(
                    new ProblemDetails
                    {
                        Detail = "CV đã thay đổi. Hãy tải lại danh sách trước khi xóa.",
                    }
                );
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
                "OnlineCvsController.Delete"
            );
            throw;
        }
    }
}
