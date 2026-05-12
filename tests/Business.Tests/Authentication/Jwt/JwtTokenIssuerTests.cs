using Business.Authentication.Jwt;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Business.Tests.Authentication.Jwt;

public class JwtTokenIssuerTests
{
    private const string SigningKey = "test-signing-key-must-be-at-least-32-bytes-long-for-hmac-sha256";

    [Fact]
    public async Task Issue_returns_token_with_expected_claims()
    {
        var options = Snapshot(new JwtOptions
        {
            SigningKey = SigningKey,
            Issuer = "test-iss",
            Audience = "test-aud",
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
        });
        var issuer = new JwtTokenIssuer(options);
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "alice@example.com",
            Username = "alice",
            DisplayName = "Alice",
        };

        var token = issuer.Issue(user);

        Assert.False(string.IsNullOrEmpty(token.AccessToken));
        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token.AccessToken, new TokenValidationParameters
        {
            ValidIssuer = "test-iss",
            ValidAudience = "test-aud",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateLifetime = false,
        });

        Assert.True(result.IsValid);
        Assert.Equal(user.Id.ToString(), result.Claims["sub"]);
        Assert.Equal(user.TenantId.ToString(), result.Claims["tid"]);
        Assert.Equal("alice@example.com", result.Claims["email"]);
        Assert.Equal("alice", result.Claims["preferred_username"]);
        Assert.Equal("Alice", result.Claims["name"]);
    }

    [Fact]
    public void Issue_throws_when_signing_key_missing()
    {
        var issuer = new JwtTokenIssuer(Snapshot(new JwtOptions { SigningKey = null }));

        Assert.Throws<InvalidOperationException>(() => issuer.Issue(new User { Id = Guid.NewGuid() }));
    }

    [Fact]
    public void Issue_throws_when_signing_key_too_short()
    {
        var issuer = new JwtTokenIssuer(Snapshot(new JwtOptions { SigningKey = "too-short" }));

        Assert.Throws<InvalidOperationException>(() => issuer.Issue(new User { Id = Guid.NewGuid() }));
    }

    [Fact]
    public void Issue_includes_unique_jti_per_token()
    {
        var issuer = new JwtTokenIssuer(Snapshot(new JwtOptions { SigningKey = SigningKey, Issuer = "x", Audience = "x" }));
        var user = new User { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Email = "a@x", Username = "a", DisplayName = "A" };

        var token1 = issuer.Issue(user);
        var token2 = issuer.Issue(user);

        Assert.NotEqual(token1.AccessToken, token2.AccessToken);
    }

    private static IOptionsSnapshot<JwtOptions> Snapshot(JwtOptions options)
    {
        var snapshot = Substitute.For<IOptionsSnapshot<JwtOptions>>();
        snapshot.Value.Returns(options);
        return snapshot;
    }
}
