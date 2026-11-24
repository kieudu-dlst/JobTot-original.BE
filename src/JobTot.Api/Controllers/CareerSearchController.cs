using JobTot.Application.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/ai/career-search")]
[Authorize(AuthenticationSchemes = CandidateAuthentication.Scheme + "," + EmployerAuthentication.Scheme, Roles = "Candidate,Employer")]
[ServiceFilter(typeof(AiConcurrencyFilter))]
public sealed class CareerSearchController(CareerSearchService service) : ControllerBase
{
    [HttpPost, RequestSizeLimit(16384)]
    public async Task<ActionResult<CareerSearchResponse>> Search(CareerSearchRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await service.SearchAsync(request, ct));
        }
        catch (CareerSearchException e)
        {
            var status = e.Code switch
            {
                "ai_invalid_question" => 400,
                "ai_not_configured" or "ai_configuration" => 503,
                "ai_provider_quota" => 503,
                "ai_timeout" => 504,
                "ai_no_answer" => 422,
                _ => 502,
            };
            var extensions = new Dictionary<string, object?> { ["code"] = e.Code };
            if (e.FailureStage is not null) extensions["failureStage"] = e.FailureStage;
            if (e.ProviderStatus is not null) extensions["providerStatus"] = e.ProviderStatus;
            if (e.ProviderReason is not null) extensions["providerReason"] = e.ProviderReason;
            if (e.QuotaViolations.Count > 0) extensions["quotaViolations"] = e.QuotaViolations;
            return Problem(statusCode: status, title: "Trợ lý nghề nghiệp", detail: e.Message, extensions: extensions);
        }
    }
}
