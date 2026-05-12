using System.Security.Claims;
using System.Text;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Business.Authentication.Jwt;

internal sealed class JwtTokenIssuer(IOptionsSnapshot<JwtOptions> options) : IJwtTokenIssuer
{
    public IssuedToken Issue(User user, Guid? sessionId = null)
    {
        var opts = options.Value;
        if (string.IsNullOrEmpty(opts.SigningKey) || Encoding.UTF8.GetByteCount(opts.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey is missing or too short. Must be at least 32 UTF-8 bytes (HMAC-SHA256). " +
                "Set via `dotnet user-secrets set` (dev) or environment / secret store (prod). See docs/SECRETS.md.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.Add(opts.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tid", user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("preferred_username", user.Username),
            new("name", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        if (sessionId is { } sid)
        {
            claims.Add(new Claim("sid", sid.ToString()));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = opts.Issuer,
            Audience = opts.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(claims),
        };

        var handler = new JsonWebTokenHandler();
        return new IssuedToken(handler.CreateToken(descriptor), expires);
    }
}
