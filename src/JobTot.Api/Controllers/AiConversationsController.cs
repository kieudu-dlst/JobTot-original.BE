using JobTot.Application.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/ai/conversations")]
[Authorize(AuthenticationSchemes = CandidateAuthentication.Scheme + "," + EmployerAuthentication.Scheme, Roles = "Candidate,Employer")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AiConversationsController(AiConversationService conversations) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        try { return Ok(await conversations.ListAsync(ct)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return StatusCode(499); }
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Read(Guid id, [FromQuery] long? beforeId, CancellationToken ct)
    {
        try
        {
            var page = await conversations.ReadAsync(id, beforeId, ct);
            return page is null ? NotFound() : Ok(page);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return StatusCode(499); }
    }
    [HttpGet("{id:guid}/messages/{messageId:long}/image")]
    public async Task<IActionResult> Image(Guid id, long messageId, CancellationToken ct)
    {
        try
        {
            var image = await conversations.ImageAsync(id, messageId, ct);
            return image is null ? NotFound() : File(image.Bytes, image.MimeType);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return StatusCode(499); }
    }
}
