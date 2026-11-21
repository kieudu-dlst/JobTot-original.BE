using System.Security.Claims;
using JobTot.Application.Execution;

namespace JobTot.Api;

public sealed class CurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private HttpContext Context => accessor.HttpContext ?? throw new InvalidOperationException("No active request.");
    public Guid AccountId => Guid.Parse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public Guid SessionId => Guid.Parse(Context.User.FindFirstValue("sid")!);
    public bool IsEmployer => Context.Request.Path.StartsWithSegments("/api/employer");
    public CancellationToken Cancellation => Context.RequestAborted;
}
