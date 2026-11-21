using JobTot.Application.Cvs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using JobTot.Application.Features;
using static JobTot.Application.Features.AdminCvTemplatesService;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminCvTemplatesController(AdminCvTemplatesService service) : ApplicationController
{
    [HttpPost("auth/login"), EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> Login(CandidateLoginRequest input, CancellationToken ct) =>
        ToAction(await service.Login(input, ct));
    [HttpPost("auth/logout"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> Logout(CancellationToken ct) =>
        ToAction(await service.Logout(ct));
    [HttpGet("cv-templates"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        ToAction(await service.List(ct));
    [HttpGet("cv-templates/{id:guid}"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        ToAction(await service.Get(id, ct));
    [HttpPost("cv-templates"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> Create(CvTemplateWrite input, CancellationToken ct) =>
        ToAction(await service.Create(input, ct));
    [HttpPut("cv-templates/{id:guid}"), Authorize(Policy = AdminAuthentication.Policy)]
    public async Task<IActionResult> Update(Guid id, CvTemplateWrite input, CancellationToken ct) =>
        ToAction(await service.Update(id, input, ct));
}
