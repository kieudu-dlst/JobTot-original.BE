using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.CompanyMapService;

namespace JobTot.Api.Controllers;

[ApiController]
[Authorize(Policy = EmployerAuthentication.Policy)]
[Route("api/employer/company/map")]
public sealed class CompanyMapController(CompanyMapService service) : ApplicationController
{
    [HttpGet]
    public async Task<IActionResult> Resolve([FromQuery] string url, CancellationToken ct) =>
        ToAction(await service.Resolve(url, ct));
    [AllowAnonymous]
    [HttpGet("/api/companies/map")]
    public async Task<IActionResult> ResolvePublic([FromQuery] string url, CancellationToken ct) =>
        ToAction(await service.ResolvePublic(url, ct));
}
