using JobTot.Application.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.EmployerCampaignsService;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/employer/campaigns"), Authorize(Policy = EmployerAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmployerCampaignsController(EmployerCampaignsService service) : ApplicationController
{
    [HttpGet("metadata")]
    public async Task<IActionResult> Metadata(CancellationToken ct) =>
        ToAction(await service.Metadata(ct));
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? keyword,
        [FromQuery] string? state,
        [FromQuery] int page = 1,
        CancellationToken ct = default
    ) =>
        ToAction(await service.List(keyword, state, page, ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        ToAction(await service.Get(id, ct));
    [HttpPost]
    public async Task<IActionResult> Create(EmployerCampaignRequest request, CancellationToken ct) =>
        ToAction(await service.Create(request, ct));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, EmployerCampaignRequest request, CancellationToken ct) =>
        ToAction(await service.Update(id, request, ct));
}
