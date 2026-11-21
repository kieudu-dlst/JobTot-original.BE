using JobTot.Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.EmployerCandidatesService;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/employer/candidates"), Authorize(Policy = EmployerAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmployerCandidatesController(EmployerCandidatesService service) : ApplicationController
{
    [HttpGet("metadata")]
    public async Task<IActionResult> Metadata(CancellationToken ct) =>
        ToAction(await service.Metadata(ct));
    [HttpPost("import")]
    public async Task<IActionResult> Import(CancellationToken ct) =>
        ToAction(await service.Import(ct));
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? campaignId,
        [FromQuery] Guid? stageId,
        [FromQuery] string? keyword,
        [FromQuery] string? state,
        [FromQuery] string? source,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        CancellationToken ct = default
    ) =>
        ToAction(await service.List(campaignId, stageId, keyword, state, source, sort, page, ct));
    [HttpGet("talents")]
    public async Task<IActionResult> TalentList(
        [FromQuery] string? keyword,
        [FromQuery] string? readiness,
        [FromQuery] int page = 1,
        CancellationToken ct = default
    ) =>
        ToAction(await service.TalentList(keyword, readiness, page, ct));
    [HttpPost("talents")]
    public async Task<IActionResult> AddTalent(AddTalentRequest input, CancellationToken ct) =>
        ToAction(await service.AddTalent(input, ct));
    [HttpPut("talents/{id:guid}")]
    public async Task<IActionResult> UpdateTalent(Guid id, TalentUpdate input, CancellationToken ct) =>
        ToAction(await service.UpdateTalent(id, input, ct));
    [HttpPost("talents/{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignRequest input, CancellationToken ct) =>
        ToAction(await service.Assign(id, input, ct));
    [HttpPost("move")]
    public async Task<IActionResult> Move(MoveRequest input, CancellationToken ct) =>
        ToAction(await service.Move(input, ct));
    [HttpGet("{id:guid}/cv")]
    public async Task<IActionResult> Cv(Guid id, CancellationToken ct) =>
        ToAction(await service.Cv(id, ct));
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ApplicationStatus(Guid id, StatusRequest input, CancellationToken ct) =>
        ToAction(await service.ApplicationStatus(id, input, ct));
    [HttpGet("interviews")]
    public async Task<IActionResult> Calendar(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? keyword,
        CancellationToken ct
    ) =>
        ToAction(await service.Calendar(from, to, keyword, ct));
    [HttpPost("interviews")]
    public async Task<IActionResult> Schedule(ScheduleRequest input, CancellationToken ct) =>
        ToAction(await service.Schedule(input, ct));
    [HttpPost("interviews/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) =>
        ToAction(await service.Cancel(id, ct));
    [HttpPut("interviews/{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, FeedbackRequest input, CancellationToken ct) =>
        ToAction(await service.Feedback(id, input, ct));
}
