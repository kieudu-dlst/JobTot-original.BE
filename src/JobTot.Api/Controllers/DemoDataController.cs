using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.DemoDataService;

namespace JobTot.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/demo-data")]
public sealed class DemoDataController(DemoDataService service) : ApplicationController
{
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct) =>
        ToAction(await service.Get(key, ct));
}
