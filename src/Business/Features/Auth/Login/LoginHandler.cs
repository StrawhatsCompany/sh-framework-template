using Business.Authentication;
using Business.Authentication.Jwt;
using Business.Authentication.Sessions;
using Business.Identity;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Login;

public sealed class LoginHandler(
    ITenantStore tenants,
    IUserStore users,
    IPasswordHasher hasher,
    IJwtTokenIssuer jwt,
    ISessionStore sessions,
    IRefreshTokenStore refreshTokens,
    IRefreshTokenFactory refreshTokenFactory,
    IOptionsSnapshot<LoginOptions> loginOptions,
    IOptionsSnapshot<JwtOptions> jwtOptions)
    : RequestHandler<LoginCommand, LoginResponse>
{
    public override async Task<Result<LoginResponse>> HandleAsync(
        LoginCommand request, CancellationToken cancellationToken = default)
    {
        // 1. Resolve tenant.
        var tenant = await ResolveTenantAsync(request, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<LoginResponse>(AuthResultCode.TenantNotFound);
        }
        if (tenant.Status == TenantStatus.Suspended)
        {
            return Result.Failure<LoginResponse>(AuthResultCode.TenantSuspended);
        }

        // 2. Look up user by email or username.
        var identifier = request.Identifier.Trim();
        var user = await users.FindByEmailAsync(tenant.Id, identifier, cancellationToken)
                   ?? await users.FindByUsernameAsync(tenant.Id, identifier, cancellationToken);

        if (user is null)
        {
            return Result.Failure<LoginResponse>(AuthResultCode.InvalidCredentials);
        }

        // 3. Status checks BEFORE password verify — distinct codes help the user understand why.
        switch (user.Status)
        {
            case UserStatus.Disabled:
                return Result.Failure<LoginResponse>(AuthResultCode.UserDisabled);
            case UserStatus.Locked:
                return Result.Failure<LoginResponse>(AuthResultCode.UserLocked);
            case UserStatus.PendingVerification:
                return Result.Failure<LoginResponse>(AuthResultCode.UserPendingVerification);
        }

        // 4. Password verify.
        if (string.IsNullOrEmpty(user.PasswordHash) || !hasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            user.LastFailedLoginAt = DateTime.UtcNow;
            if (user.FailedLoginAttempts >= loginOptions.Value.MaxFailedAttempts)
            {
                user.Status = UserStatus.Locked;
            }
            await users.UpdateAsync(user, cancellationToken);
            return Result.Failure<LoginResponse>(AuthResultCode.InvalidCredentials);
        }

        // 5. Success — reset counters, stamp LastLoginAt.
        user.FailedLoginAttempts = 0;
        user.LastFailedLoginAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user, cancellationToken);

        // 6. Create the Session + initial RefreshToken.
        var now = DateTime.UtcNow;
        var jwtOpts = jwtOptions.Value;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            AuthMethod = SessionAuthMethod.Password,
            DeviceLabel = string.IsNullOrWhiteSpace(request.DeviceLabel) ? null : request.DeviceLabel.Trim(),
            IpFirst = request.Ip,
            IpLast = request.Ip,
            LastSeenAt = now,
            ExpiresAt = now.Add(jwtOpts.RefreshTokenLifetime),
            Status = SessionStatus.Active,
            CreatedAt = now,
            CreatedBy = user.Id,
        };
        await sessions.AddAsync(session, cancellationToken);

        var (refreshPlain, refreshHash) = refreshTokenFactory.Generate();
        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            SessionId = session.Id,
            TokenHash = refreshHash,
            ExpiresAt = session.ExpiresAt,
            Status = RefreshTokenStatus.Active,
            CreatedAt = now,
        };
        await refreshTokens.AddAsync(refresh, cancellationToken);

        // 7. Mint the access JWT (with sid for logout / session-tracking).
        var token = jwt.Issue(user, session.Id);

        return Result.Success(new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = refreshPlain,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            SessionId = session.Id,
            UserId = user.Id,
            TenantId = tenant.Id,
        });
    }

    private async Task<Tenant?> ResolveTenantAsync(LoginCommand request, CancellationToken ct)
    {
        if (request.TenantId is { } id)
        {
            return await tenants.FindByIdAsync(id, ct);
        }
        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            return await tenants.FindBySlugAsync(request.TenantSlug.Trim().ToLowerInvariant(), ct);
        }
        return null;
    }
}
