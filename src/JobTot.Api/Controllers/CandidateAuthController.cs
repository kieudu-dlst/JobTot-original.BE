using System.Security.Claims;
using JobTot.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/candidate/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CandidateAuthController(
    CandidateAuthService service,
    CandidateTokens tokens,
    PasswordRecovery recovery,
    AccountEmailSecurity emailSecurity
) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    [ProducesResponseType<CandidateSessionDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CandidateSessionDto>> Register(
        CandidateRegisterRequest request,
        CancellationToken ct
    )
    {
        try
        {
            await emailSecurity.RequireVerification(
                request.EmailOrPhone,
                "Candidate",
                request.EmailVerificationToken,
                ct
            );
            var account = await service.RegisterAsync(request, ct);
            var session = await tokens.CreateAsync(account, rememberMe: false, ct);
            return CreatedAtAction(nameof(Me), session);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthController.Register"
            );
            throw;
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<ActionResult<CandidateSessionDto>> Login(
        CandidateLoginRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var account = await service.LoginAsync(request, ct);
            if (account is null)
            {
                var changeToken = await emailSecurity.TemporaryLogin(
                    request.EmailOrPhone,
                    "Candidate",
                    request.Password,
                    ct
                );
                if (changeToken != null)
                    return StatusCode(
                        428,
                        new { detail = "Đặt mật khẩu mới để hoàn tất đăng nhập.", changeToken }
                    );
            }
            if (account is null)
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Đăng nhập không thành công",
                    detail: "Email/số điện thoại hoặc mật khẩu không đúng, hoặc tài khoản không khả dụng."
                );
            return Ok(await tokens.CreateAsync(account, request.RememberMe, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthController.Login"
            );
            throw;
        }
    }

    [HttpGet("me")]
    [Authorize(Policy = CandidateAuthentication.Policy)]
    public async Task<ActionResult<CandidateAccountDto>> Me(CancellationToken ct)
    {
        try
        {
            var account = await service.GetActiveAsync(
                Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                ct
            );
            return account is null ? Unauthorized() : Ok(account);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthController.Me"
            );
            throw;
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken ct
    )
    {
        try
        {
            await emailSecurity.RequestTemporaryPassword(request.Email, "Candidate", ct);
            return Ok(
                new
                {
                    message = "Nếu email liên kết với tài khoản khả dụng, bạn sẽ nhận được mật khẩu tạm có hiệu lực 30 phút.",
                }
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthController.ForgotPassword"
            );
            throw;
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken ct
    )
    {
        try
        {
            await recovery.ResetAsync(request, ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthController.ResetPassword"
            );
            throw;
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<ActionResult<CandidateSessionDto>> Refresh(
        CandidateRefreshRequest request,
        CancellationToken ct
    )
    {
        try
        {
            return await tokens.RefreshAsync(request.RefreshToken, ct) is { } session
                ? Ok(session)
                : Unauthorized();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthController.Refresh"
            );
            throw;
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CandidateRefreshRequest request, CancellationToken ct)
    {
        try
        {
            await tokens.LogoutAsync(request.RefreshToken, ct);
            return NoContent();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthController.Logout"
            );
            throw;
        }
    }
}
