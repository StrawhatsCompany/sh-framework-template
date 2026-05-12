using Business.Authentication;
using Business.Authentication.Jwt;
using Business.Authentication.Mfa;
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
    IMfaFactorStore mfaFactors,
    IMfaOrchestrator mfaOrchestrator,
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

        // 3. Status checks BEFORE password verify.
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

        // 5. Password OK — reset counters before branching.
        user.FailedLoginAttempts = 0;
        user.LastFailedLoginAt = null;
        await users.UpdateAsync(user, cancellationToken);

        // 6. If the user has any Active MFA factor, branch into the half-authenticated flow.
        //    The login is NOT complete until POST /api/v1/auth/mfa/verify consumes a challenge.
        var activeFactors = await mfaFactors.ListActiveByUserAsync(tenant.Id, user.Id, cancellationToken);
        if (activeFactors.Count > 0)
        {
            // Pick the first Active factor; clients with multiple factors can re-issue against
            // a different one via the mfa/verify flow's challenge id selection (future enhancement).
            var primary = activeFactors[0];
            var challengeResult = await mfaOrchestrator.IssueAsync(tenant.Id, user.Id, primary.Id, cancellationToken);
            if (!challengeResult.IsSuccess)
            {
                return Result.Failure<LoginResponse>(ResultCode.Instance(
                    challengeResult.Code, challengeResult.CategorizedCode, challengeResult.Description));
            }
            var challenge = challengeResult.Data!;
            return Result.Success(new LoginResponse
            {
                MfaRequired = true,
                ChallengeId = challenge.Id,
                ChallengeKind = challenge.Kind,
                ChallengeExpiresAt = challenge.ExpiresAt,
            });
        }

        // 7. No MFA — stamp LastLoginAt, mint session + tokens directly.
        user.LastLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user, cancellationToken);

        var completed = await IssueSessionAndTokensAsync(tenant.Id, user, request.DeviceLabel, request.Ip,
            SessionAuthMethod.Password, cancellationToken);
        return Result.Success(completed);
    }

    private async Task<LoginResponse> IssueSessionAndTokensAsync(
        Guid tenantId, User user, string? deviceLabel, string? ip, SessionAuthMethod authMethod, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var jwtOpts = jwtOptions.Value;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            AuthMethod = authMethod,
            DeviceLabel = string.IsNullOrWhiteSpace(deviceLabel) ? null : deviceLabel.Trim(),
            IpFirst = ip,
            IpLast = ip,
            LastSeenAt = now,
            ExpiresAt = now.Add(jwtOpts.RefreshTokenLifetime),
            Status = SessionStatus.Active,
            CreatedAt = now,
            CreatedBy = user.Id,
        };
        await sessions.AddAsync(session, ct);

        var (refreshPlain, refreshHash) = refreshTokenFactory.Generate();
        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SessionId = session.Id,
            TokenHash = refreshHash,
            ExpiresAt = session.ExpiresAt,
            Status = RefreshTokenStatus.Active,
            CreatedAt = now,
        };
        await refreshTokens.AddAsync(refresh, ct);

        var token = jwt.Issue(user, session.Id);

        return new LoginResponse
        {
            MfaRequired = false,
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = refreshPlain,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            SessionId = session.Id,
            UserId = user.Id,
            TenantId = tenantId,
        };
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

    // Internal-visible helper for the MFA verify slice to complete the half-authenticated flow.
    internal Task<LoginResponse> CompleteLoginAsync(
        Guid tenantId, User user, string? deviceLabel, string? ip, CancellationToken ct) =>
        IssueSessionAndTokensAsync(tenantId, user, deviceLabel, ip, SessionAuthMethod.Password, ct);
}
