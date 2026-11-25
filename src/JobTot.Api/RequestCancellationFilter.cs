using JobTot.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JobTot.Api;

// Handle expected disconnects inside MVC, before its exception crosses framework frames.
public sealed class RequestCancellationFilter : IAsyncResourceFilter, IExceptionFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            Complete(context.HttpContext);
            context.Result = new EmptyResult();
            return;
        }
        try
        {
            var executed = await next();
            if (executed.Exception is { } exception
                && RequestCancellation.IsClientCancellation(exception, context.HttpContext.RequestAborted))
            {
                Complete(context.HttpContext);
                executed.Result = new EmptyResult();
                executed.ExceptionHandled = true;
            }
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, context.HttpContext.RequestAborted))
        {
            Complete(context.HttpContext);
            context.Result = new EmptyResult();
        }
    }

    public void OnException(ExceptionContext context)
    {
        if (!RequestCancellation.IsClientCancellation(context.Exception, context.HttpContext.RequestAborted))
            return;
        Complete(context.HttpContext);
        context.Result = new EmptyResult();
        context.ExceptionHandled = true;
    }

    private static void Complete(HttpContext context)
    {
        if (!context.Response.HasStarted)
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
    }
}
