using System.Security.Claims;
using JobTot.Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/candidate")]
[Authorize(Policy = CandidateAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CandidateJobsController(ICandidateJobsRepository repository) : ControllerBase
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("saved-jobs")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> Saved(CancellationToken ct)
    {
        try
        {
            return Ok(await repository.SavedIdsAsync(AccountId, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsController.Saved"
            );
            throw;
        }
    }

    [HttpPut("saved-jobs/{id:guid}")]
    public async Task<IActionResult> Save(Guid id, CancellationToken ct)
    {
        try
        {
            await repository.SetSavedAsync(AccountId, id, true, ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsController.Save"
            );
            throw;
        }
    }

    [HttpDelete("saved-jobs/{id:guid}")]
    public async Task<IActionResult> Unsave(Guid id, CancellationToken ct)
    {
        try
        {
            await repository.SetSavedAsync(AccountId, id, false, ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsController.Unsave"
            );
            throw;
        }
    }

    [HttpGet("my-jobs")]
    public async Task<ActionResult<CandidateJobsDashboard>> Dashboard(
        [FromQuery] CandidateJobsQuery query,
        CancellationToken ct
    )
    {
        try
        {
            return Ok(await repository.DashboardAsync(AccountId, query, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsController.Dashboard"
            );
            throw;
        }
    }

    [HttpPut("my-jobs/status")]
    public async Task<IActionResult> ReadyStatus(
        CandidateReadyStatusRequest request,
        CancellationToken ct
    )
    {
        try
        {
            await repository.SetReadyStatusAsync(AccountId, request.ReadyStatus, ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsController.ReadyStatus"
            );
            throw;
        }
    }

    [HttpPut("my-jobs/availability")]
    public async Task<IActionResult> Availability(
        CandidateAvailabilityRequest request,
        CancellationToken ct
    )
    {
        try
        {
            await repository.SetAvailabilityAsync(AccountId, request.OpenToWork, ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsController.Availability"
            );
            throw;
        }
    }

    [HttpPut("interviews/{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmInterview(Guid id, CancellationToken ct)
    {
        try
        {
            await repository.ConfirmInterviewAsync(AccountId, id, ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobsController.ConfirmInterview"
            );
            throw;
        }
    }
}
