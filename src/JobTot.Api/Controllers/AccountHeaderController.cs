using JobTot.Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.AccountHeaderService;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/candidate/header"), Route("api/employer/header")]
[Authorize(AuthenticationSchemes = CandidateAuthentication.Scheme + "," + EmployerAuthentication.Scheme)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountHeaderController(AccountHeaderService service) : ApplicationController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => ToAction(await service.Get(ct));
    [HttpPut("read")]
    public async Task<IActionResult> MarkRead(ReadInput input, CancellationToken ct) =>
        ToAction(await service.MarkRead(input, ct));
}
