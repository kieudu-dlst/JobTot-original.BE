using System.Security.Claims;
using JobTot.Application.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/candidate/followed-companies")]
[Authorize(Policy = CandidateAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FollowedCompaniesController(ICompanyDirectoryRepository directory)
    : ControllerBase
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Guid>>> List(CancellationToken ct)
    {
        try
        {
            return Ok(await directory.FollowedAsync(AccountId, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "FollowedCompaniesController.List"
            );
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Follow(Guid id, CancellationToken ct)
    {
        try
        {
            await directory.SetFollowedAsync(AccountId, id, true, ct);
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
                "FollowedCompaniesController.Follow"
            );
            throw;
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unfollow(Guid id, CancellationToken ct)
    {
        try
        {
            await directory.SetFollowedAsync(AccountId, id, false, ct);
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
                "FollowedCompaniesController.Unfollow"
            );
            throw;
        }
    }
}
