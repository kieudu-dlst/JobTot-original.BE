using JobTot.Infrastructure;
using JobTot.Application;
using JobTot.Application.Authentication;
using JobTot.Application.Profiles;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JobTot.Application.Persistence;

namespace JobTot.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IDataFailureClassifier failures) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct
    )
    {
        // A disconnect can race with exception handling. Do not log a normal cancellation
        // or attempt to serialize an error onto the already-cancelled connection.
        if (RequestCancellation.IsClientCancellation(exception, context.RequestAborted))
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }
        var (status, title) = exception switch
        {
            DataReadTimeoutException => (503, "Cơ sở dữ liệu phản hồi quá lâu. Vui lòng thử lại sau."),
            EmailSecurityException email => (email.Status, "Xác thực email"),
            EmployerCompanyConflictException => (409, "Doanh nghiệp đã tồn tại"),
            DuplicateAccountException => (409, "Tài khoản đã tồn tại"),
            CandidatePhoneConflictException => (409, "Số điện thoại đã được sử dụng"),
            InvalidCredentialsException => (401, "Đăng nhập không thành công"),
            JobApplicationConflictException => (409, "Không thể ứng tuyển"),
            NotFoundException => (404, "Resource not found"),
            ArgumentException => (400, "Invalid request"),
            InvalidOperationException when exception.Message == "Closed jobs cannot be edited." => (
                409,
                "Job is closed"
            ),
            Exception failure when failures.IsConcurrency(failure) => (409, "Data changed; reload and retry"),
            _ => (500, "An unexpected error occurred"),
        };
        if (status == 500)
            logger.LogError(
                exception,
                "Request failed: {TraceId}; operations: {Operations}",
                context.TraceIdentifier,
                exception.Data[ExceptionDiagnostics.OperationsKey] is List<string> operations
                    ? string.Join(" -> ", operations)
                    : "unrecorded"
            );
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail =
                    status < 500 || exception is EmailSecurityException ? exception.Message : null,
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier },
            },
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json",
            cancellationToken: ct
        );
        return true;
    }
}
