using System.Text.Json;

namespace JobTot.Application.Features;

public sealed class DemoDataService(
    IRecruitmentData db,
    ICurrentActor actor
) : ApplicationService(actor)
{
    public async Task<UseCaseResult> Get(string key, CancellationToken ct)
    {
        try
        {
            if (
                key
                is not (
                    "messages-candidate"
                    or "messages-employer"
                    or "auth-introduction"
                    or "profile"
                )
            )
                return NotFound();
            var payload = await db.Set<DemoDataset>()
                .ReadOnly()
                .Where(x => x.Key == key)
                .Select(x => x.Payload)
                .SingleOrDefaultAsync(ct);
            if (payload is null)
                return NotFound();
            return Success(JsonSerializer.Deserialize<JsonElement>(payload));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "DemoDataController.Get"
            );
            throw;
        }
    }
}
