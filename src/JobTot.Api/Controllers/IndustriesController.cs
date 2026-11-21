using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.IndustriesService;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/industries")]
public sealed class IndustriesController(IndustriesService service) : ApplicationController
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct) =>
        ToAction(await service.Overview(ct));
}
