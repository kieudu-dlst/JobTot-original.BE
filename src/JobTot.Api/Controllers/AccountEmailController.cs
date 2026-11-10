using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobTot.Api.Controllers;

[ApiController, Route("api/{role:regex(^(candidate|employer)$)}/auth/email")]
[EnableRateLimiting(CandidateAuthentication.RateLimitPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountEmailController(AccountEmailSecurity security) : ControllerBase
{
    private static string Role(string role) => role == "employer" ? "Employer" : "Candidate";

    [HttpPost("request")]
    public async Task<IActionResult> RequestCode(
        string role,
        EmailInput input,
        CancellationToken ct
    ) => Ok(new { challengeId = await security.RequestVerification(input.Email, Role(role), ct) });

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(string role, VerifyInput input, CancellationToken ct)
    {
        var result = await security.Verify(
            input.ChallengeId,
            input.Email,
            Role(role),
            input.Code,
            ct
        );
        if (result.Error is not null)
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Xác thực email",
                detail: result.Error
            );
        return Ok(new { verificationToken = result.Token });
    }

    [HttpPost("change-temporary-password")]
    public async Task<IActionResult> Change(string role, ChangeInput input, CancellationToken ct)
    {
        if (input.Password != input.ConfirmPassword)
            return BadRequest(new { detail = "Mật khẩu xác nhận không trùng khớp." });
        await security.ChangeTemporaryPassword(Role(role), input.Token, input.Password, ct);
        return NoContent();
    }

    public sealed record EmailInput([Required, EmailAddress, StringLength(320)] string Email);

    public sealed record VerifyInput(
        Guid ChallengeId,
        [Required, EmailAddress, StringLength(320)] string Email,
        [Required, RegularExpression("^[0-9]{6}$")] string Code
    );

    public sealed record ChangeInput(
        [Required, StringLength(5000)] string Token,
        [Required, StringLength(128, MinimumLength = 8)] string Password,
        [Required] string ConfirmPassword
    );
}
