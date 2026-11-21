using JobTot.Infrastructure;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;

namespace JobTot.Api;

public static class AdminAuthentication
{
    public const string Scheme = "AdminBearer";
    public const string Policy = "AdminOnly";

    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication().AddJwtBearer(Scheme, _ => { });
        services
            .AddOptions<JwtBearerOptions>(Scheme)
            .Configure<CandidateJwtSettings>(
                (options, settings) =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = settings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = settings.Audience + ".Admin",
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
                            var ct = context.HttpContext.RequestAborted;
                            try
                            {
                                ct.ThrowIfCancellationRequested();
                                if (
                                    !Guid.TryParse(
                                        context.Principal?.FindFirstValue(
                                            ClaimTypes.NameIdentifier
                                        ),
                                        out var id
                                    )
                                    || !Guid.TryParse(
                                        context.Principal?.FindFirstValue("sid"),
                                        out var sid
                                    )
                                )
                                {
                                    context.Fail("Invalid admin session.");
                                    return;
                                }
                                if (!await context.HttpContext.RequestServices.GetRequiredService<AdminSessions>().ValidateAsync(id, sid, ct))
                                    context.Fail("Admin session expired.");
                            }
                            catch (Exception exception)
                                when (RequestCancellation.IsClientCancellation(exception, ct))
                            {
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
                        .RequireRole("Admin")
            )
        );
        return services;
    }

}
