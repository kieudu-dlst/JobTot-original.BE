using JobTot.Application.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace JobTot.Api;

public static class EmployerAuthentication
{
    public const string Scheme = "EmployerBearer";
    public const string Policy = "EmployerOnly";

    public static IServiceCollection AddEmployerAuthentication(this IServiceCollection services)
    {
        services.AddScoped<EmployerTokens>();
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
                        ValidAudience = settings.Audience + ".Employer",
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
                                    context.Principal is null
                                    || !await context
                                        .HttpContext.RequestServices.GetRequiredService<EmployerTokens>()
                                        .ValidateAsync(context.Principal, ct)
                                )
                                    context.Fail("Employer session expired.");
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
                        .RequireRole(EmployerAuthService.EmployerRole)
            )
        );
        return services;
    }
}
