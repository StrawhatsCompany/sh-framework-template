using Business.Authentication;
using Business.Authentication.Jwt;
using Business.Authentication.Sessions;
using Business.Features.Auth.Login;
using Business.Identity;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;

namespace Business.Tests.Features.Auth;

public class LoginHandlerTests
{
    private const string Password = "correct horse battery staple";
    private const string SigningKey = "test-signing-key-must-be-at-least-32-bytes-long-for-hmac-sha256";

    [Fact]
    public async Task Login_succeeds_with_valid_email_password()
    {
        var (handler, _, user, _) = await Setup();

        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = user.Email,
            Password = Password,
        });

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Data!.AccessToken));
        Assert.Equal(user.Id, result.Data.UserId);
    }

    [Fact]
    public async Task Login_succeeds_with_valid_username_password()
    {
        var (handler, _, user, _) = await Setup();

        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = user.Username,
            Password = Password,
        });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Login_returns_TenantNotFound_when_slug_and_id_missing()
    {
        var (handler, _, user, _) = await Setup();

        var result = await handler.HandleAsync(new LoginCommand
        {
            Identifier = user.Email,
            Password = Password,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.TenantNotFound.Code, result.Code);
    }

    [Fact]
    public async Task Login_returns_InvalidCredentials_for_wrong_password()
    {
        var (handler, users, user, _) = await Setup();

        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = user.Email,
            Password = "wrong",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.InvalidCredentials.Code, result.Code);
        var reloaded = await users.FindByIdAsync(user.TenantId, user.Id);
        Assert.Equal(1, reloaded!.FailedLoginAttempts);
    }

    [Fact]
    public async Task Login_locks_user_after_max_failed_attempts()
    {
        var (handler, users, user, _) = await Setup(maxFailedAttempts: 3);

        for (var i = 0; i < 3; i++)
        {
            await handler.HandleAsync(new LoginCommand { TenantSlug = "acme", Identifier = user.Email, Password = "wrong" });
        }

        var reloaded = await users.FindByIdAsync(user.TenantId, user.Id);
        Assert.Equal(UserStatus.Locked, reloaded!.Status);
        Assert.Equal(3, reloaded.FailedLoginAttempts);
    }

    [Fact]
    public async Task Login_returns_UserLocked_for_locked_user_before_password_check()
    {
        var (handler, users, user, _) = await Setup();
        user.Status = UserStatus.Locked;
        await users.UpdateAsync(user);

        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = user.Email,
            Password = Password, // correct password — still rejected
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.UserLocked.Code, result.Code);
    }

    [Fact]
    public async Task Login_returns_UserDisabled_for_disabled_user()
    {
        var (handler, users, user, _) = await Setup();
        user.Status = UserStatus.Disabled;
        await users.UpdateAsync(user);

        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = user.Email,
            Password = Password,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.UserDisabled.Code, result.Code);
    }

    [Fact]
    public async Task Login_returns_UserPendingVerification_when_not_yet_verified()
    {
        var (handler, _, user, _) = await Setup(status: UserStatus.PendingVerification);

        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = user.Email,
            Password = Password,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.UserPendingVerification.Code, result.Code);
    }

    [Fact]
    public async Task Login_returns_TenantSuspended_for_suspended_tenant()
    {
        var (handler, _, user, tenants) = await Setup();
        var tenant = await tenants.FindBySlugAsync("acme");
        tenant!.Status = TenantStatus.Suspended;
        await tenants.UpdateAsync(tenant);

        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = user.Email,
            Password = Password,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.TenantSuspended.Code, result.Code);
    }

    [Fact]
    public async Task Login_returns_InvalidCredentials_when_user_does_not_exist()
    {
        var (handler, _, _, _) = await Setup();

        var result = await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = "ghost@x.com",
            Password = Password,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.InvalidCredentials.Code, result.Code);
    }

    [Fact]
    public async Task Login_resets_failed_counter_and_stamps_LastLoginAt_on_success()
    {
        var (handler, users, user, _) = await Setup();
        user.FailedLoginAttempts = 2;
        user.LastFailedLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user);

        await handler.HandleAsync(new LoginCommand
        {
            TenantSlug = "acme",
            Identifier = user.Email,
            Password = Password,
        });

        var reloaded = await users.FindByIdAsync(user.TenantId, user.Id);
        Assert.Equal(0, reloaded!.FailedLoginAttempts);
        Assert.Null(reloaded.LastFailedLoginAt);
        Assert.NotNull(reloaded.LastLoginAt);
    }

    private static async Task<(LoginHandler, IUserStore, User, ITenantStore)> Setup(
        UserStatus status = UserStatus.Active,
        int maxFailedAttempts = 5)
    {
        var tenants = new InMemoryTenantStore();
        var permissions = new InMemoryPermissionStore();
        var roles = new InMemoryRoleStore(permissions);
        var users = new InMemoryUserStore(roles);
        var hasher = new Argon2idPasswordHasher();

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Slug = "acme", DisplayName = "Acme",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow,
        };
        await tenants.AddAsync(tenant);

        var user = new User
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id,
            Email = "alice@example.com", Username = "alice", DisplayName = "Alice",
            PasswordHash = hasher.Hash(Password),
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
        await users.AddAsync(user);

        var jwt = new JwtTokenIssuer(SnapshotJwt());
        var loginOptions = SnapshotLogin(new LoginOptions { MaxFailedAttempts = maxFailedAttempts });
        var sessions = new InMemorySessionStore();
        var refreshTokens = new InMemoryRefreshTokenStore();
        var refreshFactory = new RefreshTokenFactory();
        var handler = new LoginHandler(
            tenants, users, hasher, jwt, sessions, refreshTokens, refreshFactory, loginOptions, SnapshotJwt());

        return (handler, users, user, tenants);
    }

    private static IOptionsSnapshot<JwtOptions> SnapshotJwt()
    {
        var snap = Substitute.For<IOptionsSnapshot<JwtOptions>>();
        snap.Value.Returns(new JwtOptions
        {
            SigningKey = SigningKey,
            Issuer = "test-iss",
            Audience = "test-aud",
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
        });
        return snap;
    }

    private static IOptionsSnapshot<LoginOptions> SnapshotLogin(LoginOptions options)
    {
        var snap = Substitute.For<IOptionsSnapshot<LoginOptions>>();
        snap.Value.Returns(options);
        return snap;
    }
}
