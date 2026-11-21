using JobTot.Application;
using JobTot.Application.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Features;
using static JobTot.Application.Features.AdminManagementService;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/admin"), Authorize(Policy = AdminAuthentication.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminManagementController(AdminManagementService service) : ApplicationController
{
    [HttpGet("auth/me")]
    public async Task<IActionResult> Me(CancellationToken ct) => ToAction(await service.Me(ct));
    [HttpGet("companies")]
    public async Task<ActionResult<AdminPage<AdminCompanyRow>>> Companies([FromQuery] AdminListQuery input, CancellationToken ct) =>
        ToAction(await service.Companies(input, ct));
    [HttpPut("companies/{id:guid}/verification")]
    public async Task<IActionResult> VerifyCompany(Guid id, AdminVerificationInput input, CancellationToken ct) =>
        ToAction(await service.VerifyCompany(id, input, ct));
    [HttpPut("companies/{id:guid}/status")]
    public async Task<IActionResult> CompanyStatus(Guid id, AdminStatusInput input, CancellationToken ct) =>
        ToAction(await service.CompanyStatus(id, input, ct));
    [HttpGet("users")]
    public async Task<ActionResult<AdminPage<AdminUserRow>>> Users([FromQuery] AdminListQuery input, CancellationToken ct) =>
        ToAction(await service.Users(input, ct));
    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UserStatus(Guid id, AdminStatusInput input, CancellationToken ct) =>
        ToAction(await service.UserStatus(id, input, ct));
    [HttpPut("users/{id:guid}/password")]
    public async Task<IActionResult> Password(Guid id, AdminPasswordInput input, CancellationToken ct) =>
        ToAction(await service.Password(id, input, ct));
    [HttpGet("industries")]
    public async Task<ActionResult<AdminPage<AdminIndustryRow>>> Industries([FromQuery] AdminListQuery input, CancellationToken ct) =>
        ToAction(await service.Industries(input, ct));
    [HttpPost("industries")]
    public async Task<IActionResult> CreateIndustry(AdminIndustryInput input, CancellationToken ct) =>
        ToAction(await service.CreateIndustry(input, ct));
    [HttpPut("industries/{id:guid}")]
    public async Task<IActionResult> UpdateIndustry(Guid id, AdminIndustryInput input, CancellationToken ct) =>
        ToAction(await service.UpdateIndustry(id, input, ct));
    [HttpPut("industries/{id:guid}/status")]
    public async Task<IActionResult> IndustryStatus(Guid id, AdminStatusInput input, CancellationToken ct) =>
        ToAction(await service.IndustryStatus(id, input, ct));
}
