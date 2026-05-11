using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Business.Libraries.Authentication.Jwt;

internal sealed class JwtTokenIssuer(IOptions<JwtOptions> options) : IJwtTokenIssuer
{
    public string Issue(IEnumerable<Claim> claims, TimeSpan? lifetime = null)
    {
        var opts = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = opts.Issuer,
            Audience = opts.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(lifetime ?? opts.AccessTokenLifetime),
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(claims),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
