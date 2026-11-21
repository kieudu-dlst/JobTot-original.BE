using JobTot.Application.Execution;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

public abstract class ApplicationController : ControllerBase
{
    protected ActionResult ToAction(UseCaseResult result)
    {
        if (result.Value is FilePayload file)
            return File(file.Content, file.ContentType, file.Name);
        if (result.Kind == ResultKind.Forbidden)
            return Forbid();
        var status = result.Kind switch
        {
            ResultKind.Success => StatusCodes.Status200OK,
            ResultKind.Created => StatusCodes.Status201Created,
            ResultKind.Completed => StatusCodes.Status204NoContent,
            ResultKind.NotFound => StatusCodes.Status404NotFound,
            ResultKind.Invalid => StatusCodes.Status400BadRequest,
            ResultKind.Unauthenticated => StatusCodes.Status401Unauthorized,
            ResultKind.Forbidden => StatusCodes.Status403Forbidden,
            ResultKind.Conflict => StatusCodes.Status409Conflict,
            ResultKind.Cancelled => StatusCodes.Status499ClientClosedRequest,
            ResultKind.ExternalFailure => StatusCodes.Status502BadGateway,
            ResultKind.ExternalTimeout => StatusCodes.Status504GatewayTimeout,
            ResultKind.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            _ => throw new InvalidOperationException("Unknown application result."),
        };
        if (result.ResourceId is { } id)
            Response.Headers.Location = Request.Path.ToString().TrimEnd('/') + "/" + id;
        if (result.Value is FailureDetails failure)
            return new ObjectResult(new ProblemDetails { Detail = failure.Detail }) { StatusCode = status };
        return result.Value is null ? StatusCode(status) : StatusCode(status, result.Value);
    }
}
