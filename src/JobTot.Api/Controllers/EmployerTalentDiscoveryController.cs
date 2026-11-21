using JobTot.Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.EmployerTalentDiscoveryService;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/employer/talent-discovery"), Authorize(Policy = EmployerAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmployerTalentDiscoveryController(EmployerTalentDiscoveryService service) : ApplicationController
{
    [HttpGet("metadata")]
    public async Task<IActionResult> Metadata(CancellationToken ct) =>
        ToAction(await service.Metadata(ct));
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DiscoveryQuery query, CancellationToken ct) =>
        ToAction(await service.List(query, ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] Guid? campaignId, CancellationToken ct) =>
        ToAction(await service.Get(id, campaignId, ct));
    [HttpPut("{id:guid}/interest")]
    public async Task<IActionResult> Interest(Guid id, InterestRequest input, CancellationToken ct) =>
        ToAction(await service.Interest(id, input, ct));
    [HttpPut("{id:guid}/saved")]
    public async Task<IActionResult> Saved(Guid id, SavedRequest input, CancellationToken ct) =>
        ToAction(await service.Saved(id, input, ct));
}
