using JobTot.Application;
using JobTot.Application.Companies;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/companies")]
public sealed class CompaniesController(
    RecruitmentService service,
    ICompanyDirectoryRepository directory
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<CompanyListingDto>>> List(
        [FromQuery] CompanySearch search,
        CancellationToken ct
    )
    {
        try
        {
            return Ok(await directory.SearchAsync(search, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CompaniesController.List"
            );
            throw;
        }
    }

    [HttpGet("filters")]
    public async Task<ActionResult<CompanyDirectoryMetadata>> Filters(CancellationToken ct)
    {
        try
        {
            return Ok(await directory.MetadataAsync(ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CompaniesController.Filters"
            );
            throw;
        }
    }

    [HttpGet("featured")]
    public async Task<ActionResult<IReadOnlyList<CompanyListingDto>>> Featured(CancellationToken ct)
    {
        try
        {
            var result = await directory.SearchAsync(
                new CompanySearch
                {
                    VerifiedOnly = true,
                    HiringOnly = true,
                    Sort = "jobs",
                    PageSize = 4,
                },
                ct
            );
            return Ok(result.Items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CompaniesController.Featured"
            );
            throw;
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> Get(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await service.GetCompanyAsync(id, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CompaniesController.Get"
            );
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create(
        CreateCompanyRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var company = await service.CreateCompanyAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = company.Id }, company);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CompaniesController.Create"
            );
            throw;
        }
    }
}
