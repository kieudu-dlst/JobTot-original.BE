using JobTot.Application.Ai;
using JobTot.Infrastructure.Ai;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JobTot.Api;

public static class AiRegistration
{
    public static IServiceCollection AddCareerSearch(this IServiceCollection services, bool persistHistory = true)
    {
        services.AddScoped<CareerSearchService>();
        services.AddScoped<ICareerSearchProvider, GeminiCareerSearch>();
        if (persistHistory)
        {
            services.AddScoped<IAiChatHistoryStore, AiChatHistoryStore>();
            services.AddScoped<AiConversationService>();
            services.AddScoped<AiJobLookup>();
        }
        services.AddSingleton<AiConcurrencyFilter>();
        services.AddHttpClient("gemini-career-search", client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.MaxResponseContentBufferSize = 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
            .RemoveAllLoggers();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            var previous = options.InvalidModelStateResponseFactory;
            options.InvalidModelStateResponseFactory = context => IsAi(context.HttpContext)
                ? new BadRequestObjectResult(new ProblemDetails
                {
                    Status = 400,
                    Title = "Câu hỏi không hợp lệ",
                    Detail = "Vui lòng gửi câu hỏi từ 1 đến 2000 ký tự trong JSON hợp lệ.",
                    Extensions = { ["code"] = "ai_invalid_question" },
                }) : previous(context);
        });
        foreach (var scheme in new[] { CandidateAuthentication.Scheme, EmployerAuthentication.Scheme })
            services.PostConfigure<JwtBearerOptions>(scheme, options =>
            {
                var previous = options.Events.OnChallenge;
                options.Events.OnChallenge = async context =>
                {
                    if (!IsAi(context.HttpContext))
                    {
                        if (previous is not null)
                            await previous(context);
                        return;
                    }
                    context.HandleResponse();
                    if (!context.Response.HasStarted)
                        await Error(401, "ai_unauthenticated", "Vui lòng đăng nhập để sử dụng trợ lý nghề nghiệp.").ExecuteAsync(context.HttpContext);
                };
                var previousForbidden = options.Events.OnForbidden;
                options.Events.OnForbidden = async context =>
                {
                    if (!IsAi(context.HttpContext))
                    {
                        if (previousForbidden is not null)
                            await previousForbidden(context);
                        return;
                    }
                    if (!context.Response.HasStarted)
                        await Error(403, "ai_forbidden", "Tài khoản không có quyền sử dụng trợ lý nghề nghiệp.").ExecuteAsync(context.HttpContext);
                };
            });
        return services;
    }
    internal static bool IsAi(HttpContext context) => context.Request.Path.Equals("/api/ai/career-search", StringComparison.OrdinalIgnoreCase);
    internal static IResult Error(int status, string code, string detail) => Results.Problem(statusCode: status,
        title: "Trợ lý nghề nghiệp", detail: detail, extensions: new Dictionary<string, object?> { ["code"] = code });
}

public sealed class AiConcurrencyFilter : IAsyncActionFilter, IDisposable
{
    private readonly SemaphoreSlim slots = new(4, 4);
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await slots.WaitAsync(0, context.HttpContext.RequestAborted))
        {
            context.HttpContext.Response.Headers.RetryAfter = "5";
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 429,
                Title = "Trợ lý nghề nghiệp đang bận",
                Detail = "Đang có nhiều câu hỏi được xử lý. Vui lòng thử lại sau.",
                Extensions = { ["code"] = "ai_busy" },
            })
            {
                StatusCode = 429
            };
            return;
        }
        try
        {
            await next();
        }
        finally { slots.Release(); }
    }
    public void Dispose() => slots.Dispose();
}
