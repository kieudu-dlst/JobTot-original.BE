using System.Text.Json;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/demo-data")]
public sealed class DemoDataController(RecruitmentDbContext db) : ControllerBase
{
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
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
                .AsNoTracking()
                .Where(x => x.Key == key)
                .Select(x => x.Payload)
                .SingleOrDefaultAsync(ct);
            if (payload is null)
                return NotFound();
            return Ok(JsonSerializer.Deserialize<JsonElement>(payload));
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
