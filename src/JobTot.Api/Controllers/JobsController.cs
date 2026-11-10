using JobTot.Application;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController(RecruitmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<JobDto>>> Search(
        [FromQuery] JobSearch query,
        CancellationToken ct
    )
    {
        try
        {
            return Ok(await service.SearchAsync(query, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "JobsController.Search"
            );
            throw;
        }
    }

    [HttpGet("filters")]
    public async Task<ActionResult<JobDirectoryMetadata>> Filters(CancellationToken ct)
    {
        try
        {
            return Ok(await service.GetJobFiltersAsync(ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "JobsController.Filters"
            );
            throw;
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDto>> Get(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await service.GetAsync(id, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(exception, "JobsController.Get");
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<JobDto>> Create(CreateJobRequest request, CancellationToken ct)
    {
        try
        {
            var job = await service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "JobsController.Create"
            );
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobDto>> Update(
        Guid id,
        SaveJobRequest request,
        CancellationToken ct
    )
    {
        try
        {
            return Ok(await service.UpdateAsync(id, request, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "JobsController.Update"
            );
            throw;
        }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        try
        {
            await service.CloseAsync(id, ct);
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
                "JobsController.Close"
            );
            throw;
        }
    }
}
