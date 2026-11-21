using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JobTot.Application.Authentication;
using Microsoft.IdentityModel.Tokens;
namespace JobTot.Infrastructure;

public sealed record CandidateJwtSettings(string Issuer, string Audience, SymmetricSecurityKey Key);
public sealed class AccessTokenIssuer(CandidateJwtSettings settings) : IAccessTokenIssuer
{
    public string Issue(string role, Guid accountId, Guid sessionId, string name, DateTimeOffset expires)
    {
        var audience = role == "Candidate" ? settings.Audience : settings.Audience + "." + role;
        var jwt = new JwtSecurityToken(settings.Issuer, audience,
            [new(ClaimTypes.NameIdentifier, accountId.ToString()), new(ClaimTypes.Role, role),
             new(ClaimTypes.Name, name), new("sid", sessionId.ToString()),
             new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            DateTime.UtcNow, expires.UtcDateTime, new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
