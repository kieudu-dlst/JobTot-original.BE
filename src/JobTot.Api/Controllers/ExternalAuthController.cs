using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/{role}/auth/external")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ExternalAuthController(ExternalAuthService service) : ControllerBase
{
    public sealed record StartRequest(string Mode, string? ReturnTo);
    public sealed record LoginRequest(bool RememberMe);
    private void RequireIntent()
    {
        if (Request.Headers["X-JobTot-External"] != "1")
            throw new EmailSecurityException(400, "Yêu cầu xác thực không hợp lệ.");
    }
    private string? Cookie(Guid id) => Request.Cookies[ExternalAuthService.CookieName(id)];

    [HttpPost("{provider}/start"), EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> Start(string role, string provider, StartRequest request, CancellationToken ct)
    {
        ExternalIdentityClient.AllowedProvider(provider, role);
        RequireIntent();
        var (row, secret, url) = await service.Start(role, provider, request.Mode, request.ReturnTo, ct);
        Response.Cookies.Append(ExternalAuthService.CookieName(row.Id), secret, new CookieOptions
        {
            HttpOnly = true,
            Secure = service.CallbackUrl(role, provider).StartsWith("https://", StringComparison.Ordinal),
            SameSite = SameSiteMode.Lax,
            Path = "/api",
            Expires = row.ExpiresAt,
            IsEssential = true,
        });
        return Ok(new
        {
            authorizationUrl = url
        });
    }

    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback(string role, string provider, string? state, string? code,
        string? error, CancellationToken ct)
    {
        ExternalIdentityClient.AllowedProvider(provider, role);
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var id = state?.Split('.').FirstOrDefault();
        if (!Guid.TryParseExact(id, "N", out var flowId))
            return Redirect(service.FrontendUrl(role, error: "invalid_state"));
        try
        {
            await service.Callback(role, provider, state!, Cookie(flowId), code, error, ct);
            return Redirect(service.FrontendUrl(role, flowId));
        }
        catch (Exception exception) when (exception is EmailSecurityException or Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Redirect(service.FrontendUrl(role, error: "invalid_state"));
        }
    }

    [HttpGet("flows/{id:guid}")]
    public async Task<IActionResult> Flow(string role, Guid id, CancellationToken ct) =>
        Ok(await service.Describe(role, id, Cookie(id), ct));

    [HttpPost("flows/{id:guid}/registration"), EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> Registration(string role, Guid id, CancellationToken ct)
    {
        RequireIntent();
        return Ok(await service.ConfirmRegistration(role, id, Cookie(id), ct));
    }

    [HttpPost("flows/{id:guid}/register"), EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> Register(string role, Guid id, ExternalRegistrationRequest request, CancellationToken ct)
    {
        RequireIntent();
        return Ok(await service.Register(role, id, Cookie(id), request, ct));
    }

    [HttpPost("flows/{id:guid}/session"), EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> Session(string role, Guid id, LoginRequest request, CancellationToken ct)
    {
        RequireIntent();
        return Ok(await service.Login(role, id, Cookie(id), request.RememberMe, ct));
    }

    [HttpPost("flows/{id:guid}/link"), EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> Link(string role, Guid id, ExternalLinkRequest request, CancellationToken ct)
    {
        RequireIntent();
        try
        {
            return Ok(await service.ConfirmLink(role, id, Cookie(id), request, ct));
        }
        catch (ExternalPasswordChangeException change)
        {
            return StatusCode(428, new
            {
                detail = "Đặt mật khẩu mới để hoàn tất liên kết.",
                changeToken = change.Token
            });
        }
    }

    [HttpPost("flows/{id:guid}/link-session")]
    [Authorize(AuthenticationSchemes = CandidateAuthentication.Scheme + "," + EmployerAuthentication.Scheme)]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> LinkSession(string role, Guid id, ExternalSessionLinkRequest request, CancellationToken ct)
    {
        RequireIntent();
        if (!User.IsInRole(ExternalIdentityClient.Role(role)))
            return Forbid();
        return Ok(await service.ConfirmSessionLink(role, id, Cookie(id),
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), request, ct));
    }
}
