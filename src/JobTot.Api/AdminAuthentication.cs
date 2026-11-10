using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JobTot.Api;

public static class AdminAuthentication
{
    public const string Scheme = "AdminBearer";
    public const string Policy = "AdminOnly";

    public static string Stamp(string hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));

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
                                var db =
                                    context.HttpContext.RequestServices.GetRequiredService<RecruitmentDbContext>();
                                var credentials = await (
                                    from s in db.CandidateRefreshSessions.AsNoTracking()
                                    join a in db.AccountRecords.AsNoTracking()
                                        on s.AccountId equals a.Id
                                    where
                                        s.Id == sid
                                        && a.Id == id
                                        && s.RevokedAt == null
                                        && s.ExpiresAt > DateTimeOffset.UtcNow
                                        && a.AccountType == "Admin"
                                        && a.AccountStatus == "Active"
                                        && a.Status == EntityStatus.Active
                                    select new { a.PasswordHash, s.PasswordStamp }
                                ).SingleOrDefaultAsync(ct);
                                if (
                                    credentials?.PasswordHash is not { } hash
                                    || credentials.PasswordStamp != Stamp(hash)
                                )
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

    public static string Issue(
        CandidateJwtSettings settings,
        Guid accountId,
        Guid sessionId,
        string name,
        DateTimeOffset expires
    )
    {
        var jwt = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience + ".Admin",
            [
                new(ClaimTypes.NameIdentifier, accountId.ToString()),
                new(ClaimTypes.Role, "Admin"),
                new(ClaimTypes.Name, name),
                new("sid", sessionId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            DateTime.UtcNow,
            expires.UtcDateTime,
            new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
