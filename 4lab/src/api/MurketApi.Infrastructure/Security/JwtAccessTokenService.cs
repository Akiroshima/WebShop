using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using MurketApi.Application.Abstractions;

namespace MurketApi.Infrastructure.Security;

/// <summary>Выпуск access-JWT (HS256): claims sub (id пользователя) и role.</summary>
public sealed class JwtAccessTokenService(string secretKey, TimeSpan ttl) : IAccessTokenService
{
    public (string Token, DateTimeOffset ExpiresAt) Issue(string userId, string role)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(ttl);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "murket",
            audience: "murket-web",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ],
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
