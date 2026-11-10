using System.Globalization;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using JobTot.Application.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace JobTot.Api;

public static class CandidateAuthentication
{
    public const string Scheme = "CandidateBearer";
    public const string Policy = "CandidateOnly";
    public const string RateLimitPolicy = "CandidateAuth";

    public static IServiceCollection AddCandidateAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool development
    )
    {
        var encodedKey = configuration["Jwt:SigningKey"];
        var key = string.IsNullOrWhiteSpace(encodedKey)
            ? development
                ? RandomNumberGenerator.GetBytes(32)
                : throw new InvalidOperationException(
                    "Jwt:SigningKey must be a Base64-encoded random key of at least 32 bytes."
                )
            : Convert.FromBase64String(encodedKey);
        if (key.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
        var settings = new CandidateJwtSettings(
            configuration["Jwt:Issuer"] ?? "JobTot.Api",
            configuration["Jwt:Audience"] ?? "JobTot.Web",
            new SymmetricSecurityKey(key)
        );
        services.AddSingleton(settings);
        services.AddScoped<CandidateTokens>();
        services.AddDataProtection();
        services
            .AddAuthentication(Scheme)
            .AddJwtBearer(
                Scheme,
                options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = settings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = settings.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = settings.Key,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var tokens =
                                context.HttpContext.RequestServices.GetRequiredService<CandidateTokens>();
                            try
                            {
                                if (context.HttpContext.RequestAborted.IsCancellationRequested)
                                {
                                    context.NoResult();
                                    return;
                                }
                                if (
                                    !await tokens.ValidateAsync(
                                        context.Principal!,
                                        context.HttpContext.RequestAborted
                                    )
                                )
                                    context.Fail("The login session is no longer valid.");
                            }
                            catch (Exception exception)
                                when (RequestCancellation.IsClientCancellation(
                                        exception,
                                        context.HttpContext.RequestAborted
                                    )
                                )
                            {
                                // Handle inside the JWT event, before JwtBearer logs/rethrows an authentication failure.
                                context.NoResult();
                            }
                        },
                    };
                }
            );
        services.AddAuthorization(options =>
            options.AddPolicy(
                Policy,
                policy =>
                    policy
                        .AddAuthenticationSchemes(Scheme)
                        .RequireAuthenticatedUser()
                        .RequireRole(CandidateAuthService.CandidateRole)
            )
        );
        services.AddCors(options =>
            options.AddPolicy(
                "CandidateWeb",
                policy =>
                {
                    var origins =
                        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                    if (origins.Length > 0)
                        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                }
            )
        );
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                RateLimitPolicy,
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }
                    )
            );
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(
                            retry.TotalSeconds
                        )
                        .ToString(CultureInfo.InvariantCulture);
                await Results
                    .Problem(statusCode: 429, title: "Quá nhiều yêu cầu. Vui lòng thử lại sau.")
                    .ExecuteAsync(context.HttpContext);
            };
        });
        return services;
    }
}
