using JobTot.Application.Cvs;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/cv-templates")]
public sealed class CvTemplatesController(ICvRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        try
        {
            return Ok(await repository.TemplatesAsync(false, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvTemplatesController.List"
            );
            throw;
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        try
        {
            return await repository.TemplateAsync(id, false, ct) is { } t ? Ok(t) : NotFound();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CvTemplatesController.Get"
            );
            throw;
        }
    }
}
