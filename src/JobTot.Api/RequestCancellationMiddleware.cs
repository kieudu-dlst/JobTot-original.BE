namespace JobTot.Api;

public sealed class RequestCancellationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.RequestAborted.IsCancellationRequested)
        {
            CompleteAbortedRequest(context);
            return;
        }
        try
        {
            await next(context);
        }
        catch (Exception exception)
            when (RequestCancellation.IsClientCancellation(exception, context.RequestAborted))
        {
            CompleteAbortedRequest(context);
        }
        catch (HttpRequestException) when (context.RequestAborted.IsCancellationRequested)
        {
            CompleteAbortedRequest(context);
        }
        catch (IOException exception)
            when (context.RequestAborted.IsCancellationRequested
                && exception.InnerException is System.Net.Sockets.SocketException
            )
        {
            CompleteAbortedRequest(context);
        }
    }

    private static void CompleteAbortedRequest(HttpContext context)
    {
        // The client has disconnected. Do not write a problem response to the cancelled connection.
        if (!context.Response.HasStarted)
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
    }
}
