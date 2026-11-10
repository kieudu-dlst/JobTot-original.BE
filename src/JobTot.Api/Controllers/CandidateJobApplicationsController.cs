using System.Security.Claims;
using JobTot.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/candidate/jobs/{id:guid}/application")]
[Authorize(Policy = CandidateAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CandidateJobApplicationsController(IRecruitmentRepository repository)
    : ControllerBase
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<JobSubmissionDto?>> Get(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await repository.GetSubmissionAsync(AccountId, id, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobApplicationsController.Get"
            );
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<JobSubmissionDto>> Submit(
        Guid id,
        SubmitJobApplicationRequest input,
        CancellationToken ct
    )
    {
        try
        {
            var submission = await repository.SubmitAsync(AccountId, id, input, ct);
            return CreatedAtAction(nameof(Get), new { id }, submission);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateJobApplicationsController.Submit"
            );
            throw;
        }
    }
}
