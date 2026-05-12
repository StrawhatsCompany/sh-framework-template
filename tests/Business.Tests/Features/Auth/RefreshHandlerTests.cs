using Business.Authentication;
using Business.Authentication.Jwt;
using Business.Authentication.Sessions;
using Business.Features.Auth.Refresh;
using Business.Identity;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;

namespace Business.Tests.Features.Auth;

public class RefreshHandlerTests
{
    private const string SigningKey = "test-signing-key-must-be-at-least-32-bytes-long-for-hmac-sha256";

    [Fact]
    public async Task Refresh_rotates_token_chain_and_returns_new_pair()
    {
        var scope = await Setup();

        var first = await scope.handler.HandleAsync(new RefreshCommand { RefreshToken = scope.initialToken });

        Assert.True(first.IsSuccess);
        Assert.NotEqual(scope.initialToken, first.Data!.RefreshToken);

        // Subsequent refresh with the NEW token also succeeds.
        var second = await scope.handler.HandleAsync(new RefreshCommand { RefreshToken = first.Data.RefreshToken });
        Assert.True(second.IsSuccess);
    }

    [Fact]
    public async Task Refresh_with_unknown_token_returns_NotFound()
    {
        var scope = await Setup();

        var result = await scope.handler.HandleAsync(new RefreshCommand { RefreshToken = "garbage-token" });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.RefreshTokenNotFound.Code, result.Code);
    }

    [Fact]
    public async Task Refresh_replay_of_consumed_token_triggers_family_invalidation()
    {
        var scope = await Setup();

        // First refresh consumes the initial token and issues a new one.
        var rotated = await scope.handler.HandleAsync(new RefreshCommand { RefreshToken = scope.initialToken });
        Assert.True(rotated.IsSuccess);

        // Replay: present the OLD token again.
        var replay = await scope.handler.HandleAsync(new RefreshCommand { RefreshToken = scope.initialToken });

        Assert.False(replay.IsSuccess);
        Assert.Equal(AuthResultCode.RefreshTokenReused.Code, replay.Code);

        // The new token (which was valid until now) is also revoked because the family was burned.
        var followup = await scope.handler.HandleAsync(new RefreshCommand { RefreshToken = rotated.Data!.RefreshToken });
        Assert.False(followup.IsSuccess);
        Assert.Equal(AuthResultCode.RefreshTokenReused.Code, followup.Code);

        // Session is revoked too.
        var session = await scope.sessions.FindByIdAsync(scope.tenantId, scope.sessionId);
        Assert.Equal(SessionStatus.Revoked, session!.Status);
        Assert.Equal("family-invalidation", session.RevokedReason);
    }

    [Fact]
    public async Task Refresh_against_revoked_session_fails()
    {
        var scope = await Setup();
        await scope.sessions.RevokeAsync(scope.tenantId, scope.sessionId, "admin-revoke");

        var result = await scope.handler.HandleAsync(new RefreshCommand { RefreshToken = scope.initialToken });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.SessionRevoked.Code, result.Code);
    }

    [Fact]
    public async Task Refresh_against_inactive_user_revokes_session()
    {
        var scope = await Setup();
        scope.user.Status = UserStatus.Disabled;
        await scope.users.UpdateAsync(scope.user);

        var result = await scope.handler.HandleAsync(new RefreshCommand { RefreshToken = scope.initialToken });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.InvalidCredentials.Code, result.Code);
        var session = await scope.sessions.FindByIdAsync(scope.tenantId, scope.sessionId);
        Assert.Equal(SessionStatus.Revoked, session!.Status);
    }

    private static async Task<(
        RefreshHandler handler, string initialToken, Guid tenantId, Guid sessionId,
        InMemoryUserStore users, InMemorySessionStore sessions, User user)> Setup()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var permissions = new InMemoryPermissionStore();
        var roles = new InMemoryRoleStore(permissions);
        var users = new InMemoryUserStore(roles);
        var sessions = new InMemorySessionStore();
        var refreshTokens = new InMemoryRefreshTokenStore();
        var factory = new RefreshTokenFactory();

        var user = new User
        {
            Id = userId, TenantId = tenantId, Email = "u@x", Username = "u", DisplayName = "U",
            Status = UserStatus.Active, CreatedAt = DateTime.UtcNow,
        };
        await users.AddAsync(user);

        var session = new Session
        {
            Id = sessionId, TenantId = tenantId, UserId = userId, AuthMethod = SessionAuthMethod.Password,
            LastSeenAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        await sessions.AddAsync(session);

        var (plain, hash) = factory.Generate();
        await refreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(), TenantId = tenantId, SessionId = sessionId,
            TokenHash = hash, ExpiresAt = session.ExpiresAt,
            Status = RefreshTokenStatus.Active, CreatedAt = DateTime.UtcNow,
        });

        var jwtOpts = Substitute.For<IOptionsSnapshot<JwtOptions>>();
        jwtOpts.Value.Returns(new JwtOptions
        {
            SigningKey = SigningKey, Issuer = "i", Audience = "a",
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
        });
        var issuer = new JwtTokenIssuer(jwtOpts);

        var handler = new RefreshHandler(users, sessions, refreshTokens, factory, issuer);
        return (handler, plain, tenantId, sessionId, users, sessions, user);
    }
}
