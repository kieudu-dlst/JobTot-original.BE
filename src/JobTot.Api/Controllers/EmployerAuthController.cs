using System.Security.Claims;
using JobTot.Application.Authentication;
using JobTot.Domain.Enums;
using JobTot.Application.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;


namespace JobTot.Api.Controllers;

[ApiController]
[Route("api/employer/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmployerAuthController(
    EmployerAuthService service,
    EmployerAccess access,
    EmployerTokens tokens,
    EmployerPasswordRecovery recovery,
    AccountEmailSecurity emailSecurity

) : ControllerBase
{
    [HttpGet("provinces")]
    [AllowAnonymous]
    public async Task<IActionResult> Provinces([FromServices] ProvinceDirectory directory, CancellationToken ct) =>
        Ok(await directory.ListAsync(ct));

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    [ProducesResponseType<EmployerSessionDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployerSessionDto>> Register(
        EmployerRegisterRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var result = await access.RegisterAsync(request, ct);
            if (result.Status == EmployerRegistrationStatus.DuplicateAccount)
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Tài khoản đã tồn tại",
                    detail: "Email hoặc số điện thoại đã được đăng ký. Vui lòng đăng nhập hoặc dùng thông tin khác."
                );
            if (result.Status == EmployerRegistrationStatus.CompanyConflict)
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Doanh nghiệp đã tồn tại",
                    detail: "Mã số thuế đã được đăng ký. Vui lòng liên hệ quản trị viên doanh nghiệp để được thêm tài khoản HR."
                );
            var session = result.Session!;
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
                "EmployerAuthController.Register"
            );
            throw;
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmployerSessionDto>> Login(
        EmployerLoginRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var result = await access.LoginAsync(request, ct);
            if (result.ChangeToken is { } changeToken)
                return StatusCode(428, new
                {
                    detail = "Đặt mật khẩu mới để hoàn tất đăng nhập.",
                    changeToken
                });
            if (result.Session is null)
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Đăng nhập không thành công",
                    detail: "Email công việc hoặc mật khẩu không đúng, hoặc tài khoản nhà tuyển dụng không khả dụng."
                );
            return Ok(result.Session);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerAuthController.Login"
            );
            throw;
        }
    }

    [HttpGet("me")]
    [Authorize(Policy = EmployerAuthentication.Policy)]
    public async Task<ActionResult<EmployerAccountDto>> Me(CancellationToken ct)
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
                "EmployerAuthController.Me"
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
            var result = await emailSecurity.RequestTemporaryPassword(request.Email, "Employer", ct);
            if (result.RateLimited)
                return AccountEmailController.EmailCooldown(this, result.RetryAfterSeconds);
            return Ok(
                new
                {
                    message = "Nếu email liên kết với tài khoản khả dụng, bạn sẽ nhận được mật khẩu tạm có hiệu lực 10 phút.",
                    retryAfterSeconds = result.RetryAfterSeconds,
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
                "EmployerAuthController.ForgotPassword"
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
                "EmployerAuthController.ResetPassword"
            );
            throw;
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
    public async Task<ActionResult<EmployerSessionDto>> Refresh(
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
                "EmployerAuthController.Refresh"
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
                "EmployerAuthController.Logout"
            );
            throw;
        }
    }
}
