using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.AccountMessagesService;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/candidate/messages"), Route("api/employer/messages")]
[Authorize(AuthenticationSchemes = CandidateAuthentication.Scheme + "," + EmployerAuthentication.Scheme)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountMessagesController(AccountMessagesService service, ChatSockets sockets) : ApplicationController
{
    [HttpPost("socket-ticket")]
    public IActionResult Ticket() => Ok(new { ticket = sockets.Ticket(User) });

    [AllowAnonymous, HttpGet("socket")]
    public async Task Socket([FromQuery] string? ticket)
    {
        var user = sockets.Consume(ticket);
        var role = Request.Path.StartsWithSegments("/api/employer") ? "Employer" : "Candidate";
        if (user is null || !user.IsInRole(role))
        {
            Response.StatusCode = 401;
            return;
        }
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Response.StatusCode = 400;
            return;
        }
        HttpContext.User = user;
        await sockets.Run(HttpContext, user);
    }

    [HttpGet("people")]
    public async Task<IActionResult> People([FromQuery] string? q, CancellationToken ct) =>
        ToAction(await service.People(q, ct));
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => ToAction(await service.List(ct));
    [HttpPost]
    public async Task<IActionResult> Start(StartInput input, CancellationToken ct) =>
        ToAction(await service.Start(input, ct));
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, MessageReadInput input, CancellationToken ct) =>
        ToAction(await service.MarkRead(id, input, ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Messages(Guid id, [FromQuery] long? before, [FromQuery] long? after, CancellationToken ct) =>
        ToAction(await service.Messages(id, before, after, ct));
    [HttpGet("{id:guid}/typing")]
    public async Task<IActionResult> Typing(Guid id, CancellationToken ct) =>
        ToAction(await service.Typing(id, ct));
    [HttpPost("{id:guid}/typing")]
    public async Task<IActionResult> UpdateTyping(Guid id, TypingInput input, CancellationToken ct) =>
        ToAction(await service.UpdateTyping(id, input, ct));
    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Send(Guid id, SendInput input, CancellationToken ct) =>
        ToAction(await service.Send(id, input, ct));
}
