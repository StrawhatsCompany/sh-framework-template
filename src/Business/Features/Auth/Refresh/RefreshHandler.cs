using Business.Authentication;
using Business.Authentication.Jwt;
using Business.Authentication.Sessions;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Refresh;

public sealed class RefreshHandler(
    IUserStore users,
    ISessionStore sessions,
    IRefreshTokenStore refreshTokens,
    IRefreshTokenFactory refreshTokenFactory,
    IJwtTokenIssuer jwt)
    : RequestHandler<RefreshCommand, RefreshResponse>
{
    public override async Task<Result<RefreshResponse>> HandleAsync(
        RefreshCommand request, CancellationToken cancellationToken = default)
    {
        var hash = refreshTokenFactory.Hash(request.RefreshToken);
        var stored = await refreshTokens.FindByHashAsync(hash, cancellationToken);
        if (stored is null)
        {
            return Result.Failure<RefreshResponse>(AuthResultCode.RefreshTokenNotFound);
        }

        var now = DateTime.UtcNow;

        // Replay detection: a Consumed (rotated) or Revoked token presented again means the family
        // is compromised. Burn every token in the chain AND revoke the session.
        if (stored.Status is RefreshTokenStatus.Rotated or RefreshTokenStatus.Revoked)
        {
            await refreshTokens.RevokeFamilyAsync(stored.Id, cancellationToken);
            await sessions.RevokeAsync(stored.TenantId, stored.SessionId, "family-invalidation", cancellationToken);
            return Result.Failure<RefreshResponse>(AuthResultCode.RefreshTokenReused);
        }

        if (stored.ExpiresAt <= now)
        {
            stored.Status = RefreshTokenStatus.Revoked;
            await refreshTokens.UpdateAsync(stored, cancellationToken);
            return Result.Failure<RefreshResponse>(AuthResultCode.RefreshTokenExpired);
        }

        var session = await sessions.FindByIdAsync(stored.TenantId, stored.SessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<RefreshResponse>(AuthResultCode.SessionNotFound);
        }
        if (session.Status != SessionStatus.Active)
        {
            return Result.Failure<RefreshResponse>(AuthResultCode.SessionRevoked);
        }
        if (session.ExpiresAt <= now)
        {
            session.Status = SessionStatus.Expired;
            await sessions.UpdateAsync(session, cancellationToken);
            return Result.Failure<RefreshResponse>(AuthResultCode.SessionExpired);
        }

        var user = await users.FindByIdAsync(session.TenantId, session.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            await sessions.RevokeAsync(session.TenantId, session.Id, "user-inactive", cancellationToken);
            return Result.Failure<RefreshResponse>(AuthResultCode.InvalidCredentials);
        }

        // Rotate: mint the new pair, then mark the old refresh consumed with ReplacedById set so
        // future presentations of the old token detect reuse.
        var (newPlain, newHash) = refreshTokenFactory.Generate();
        var newRefresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = session.TenantId,
            SessionId = session.Id,
            TokenHash = newHash,
            ExpiresAt = session.ExpiresAt,   // refresh tokens inherit session's absolute deadline
            Status = RefreshTokenStatus.Active,
            CreatedAt = now,
        };
        await refreshTokens.AddAsync(newRefresh, cancellationToken);

        stored.Status = RefreshTokenStatus.Rotated;
        stored.ConsumedAt = now;
        stored.ReplacedById = newRefresh.Id;
        await refreshTokens.UpdateAsync(stored, cancellationToken);

        session.LastSeenAt = now;
        session.IpLast = request.Ip ?? session.IpLast;
        await sessions.UpdateAsync(session, cancellationToken);

        var token = jwt.Issue(user, session.Id);

        return Result.Success(new RefreshResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = newPlain,
            RefreshTokenExpiresAt = newRefresh.ExpiresAt,
            SessionId = session.Id,
        });
    }
}
