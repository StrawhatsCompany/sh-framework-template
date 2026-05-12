using Business.Authentication;
using Business.Authentication.Jwt;
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
    IOptionsSnapshot<LoginOptions> loginOptions)
    : RequestHandler<LoginCommand, LoginResponse>
{
    public override async Task<Result<LoginResponse>> HandleAsync(
        LoginCommand request, CancellationToken cancellationToken = default)
    {
        // 1. Resolve tenant. TenantId wins; fall back to slug; otherwise fail.
        var tenant = await ResolveTenantAsync(request, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<LoginResponse>(AuthResultCode.TenantNotFound);
        }
        if (tenant.Status == TenantStatus.Suspended)
        {
            return Result.Failure<LoginResponse>(AuthResultCode.TenantSuspended);
        }

        // 2. Look up user by email or username (whichever matches; identifier is one or the other).
        var identifier = request.Identifier.Trim();
        var user = await users.FindByEmailAsync(tenant.Id, identifier, cancellationToken)
                   ?? await users.FindByUsernameAsync(tenant.Id, identifier, cancellationToken);

        // Generic InvalidCredentials response either way so we don't leak whether the user exists.
        if (user is null)
        {
            return Result.Failure<LoginResponse>(AuthResultCode.InvalidCredentials);
        }

        // 3. Status checks come BEFORE the password check so a locked/disabled user gets the right
        //    error code (helps the user understand why login failed and reduces support load).
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

        // 5. Success — reset counters, stamp LastLoginAt, mint the token.
        user.FailedLoginAttempts = 0;
        user.LastFailedLoginAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user, cancellationToken);

        var token = jwt.Issue(user);

        return Result.Success(new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
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
