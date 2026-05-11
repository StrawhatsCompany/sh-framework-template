using System.Security.Claims;
using System.Text;
using Business.Libraries.Authentication.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Business.Tests.Libraries.Authentication.Jwt;

public class JwtTokenIssuerTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "https://issuer.test",
        Audience = "audience.test",
        SigningKey = "test-signing-key-of-at-least-32-bytes-long-for-hmacsha256",
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
        ClockSkew = TimeSpan.FromSeconds(5),
    };

    [Fact]
    public async Task Issued_token_validates_against_the_same_parameters()
    {
        var issuer = new JwtTokenIssuer(Microsoft.Extensions.Options.Options.Create(Options));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim("permissions", "orders.read"),
        };

        var token = issuer.Issue(claims);
        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = Options.ClockSkew,
        });

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal("user-1", result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("orders.read", result.ClaimsIdentity.FindFirst("permissions")?.Value);
    }

    [Fact]
    public async Task Token_signed_with_a_different_key_fails_validation()
    {
        var issuer = new JwtTokenIssuer(Microsoft.Extensions.Options.Options.Create(Options));
        var token = issuer.Issue([new Claim(ClaimTypes.NameIdentifier, "user-2")]);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("different-key-which-is-also-at-least-32-bytes-long-zzzzzzzz")),
            ValidateLifetime = true,
            ClockSkew = Options.ClockSkew,
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Custom_lifetime_overrides_the_options_default()
    {
        var issuer = new JwtTokenIssuer(Microsoft.Extensions.Options.Options.Create(Options));

        var token = issuer.Issue([new Claim(ClaimTypes.NameIdentifier, "user-3")], lifetime: TimeSpan.FromMinutes(1));

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        var lifetime = jwt.ValidTo - jwt.ValidFrom;
        Assert.InRange(lifetime.TotalSeconds, 58, 62);
    }
}
