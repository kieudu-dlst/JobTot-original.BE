using JobTot.Application.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.EmployerCompanyService;

namespace JobTot.Api.Controllers;

[ApiController]
[Authorize(Policy = EmployerAuthentication.Policy)]
[Route("api/employer/company")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmployerCompanyController(EmployerCompanyService service) : ApplicationController
{
    [HttpGet("industries")]
    public async Task<IActionResult> Industries(CancellationToken ct) =>
        ToAction(await service.Industries(ct));
    [HttpGet]
    public async Task<ActionResult<EmployerCompanyResponse>> Get(CancellationToken ct) =>
        ToAction(await service.Get(ct));
    [HttpPut]
    [RequestSizeLimit(512 * 1024)]
    public async Task<ActionResult<EmployerCompanyResponse>> Save(EmployerCompanyDocument document, CancellationToken ct) =>
        ToAction(await service.Save(document, ct));
    [HttpPost("images/{kind}")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<EmployerCompanyResponse>> UploadImage(string kind, IFormFile file, [FromForm] string revision, CancellationToken ct) =>
        ToAction(await service.UploadImage(kind, new UploadFile(file.Length, file.FileName, file.ContentType, file.CopyToAsync), revision, ct));
}
