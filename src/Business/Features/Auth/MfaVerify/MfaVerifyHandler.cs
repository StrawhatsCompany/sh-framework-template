using Business.Authentication;
using Business.Authentication.Jwt;
using Business.Authentication.Mfa;
using Business.Authentication.Sessions;
using Business.Identity;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.MfaVerify;

public sealed class MfaVerifyHandler(
    IMfaChallengeStore challenges,
    IMfaOrchestrator orchestrator,
    IUserStore users,
    IJwtTokenIssuer jwt,
    ISessionStore sessions,
    IRefreshTokenStore refreshTokens,
    IRefreshTokenFactory refreshTokenFactory,
    IOptionsSnapshot<JwtOptions> jwtOptions)
    : RequestHandler<MfaVerifyCommand, MfaVerifyResponse>
{
    public override async Task<Result<MfaVerifyResponse>> HandleAsync(
        MfaVerifyCommand request, CancellationToken cancellationToken = default)
    {
        // Cross-tenant lookup — caller is unauthenticated; tenant comes from the challenge row.
        var challenge = await challenges.FindByIdAsync(request.ChallengeId, cancellationToken);
        if (challenge is null)
        {
            return Result.Failure<MfaVerifyResponse>(MfaResultCode.ChallengeNotFound);
        }

        var verify = await orchestrator.VerifyAsync(challenge.TenantId, challenge.Id, request.Code, cancellationToken);
        if (!verify.IsSuccess)
        {
            return Result.Failure<MfaVerifyResponse>(ResultCode.Instance(verify.Code, verify.CategorizedCode, verify.Description));
        }

        var user = await users.FindByIdAsync(challenge.TenantId, challenge.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return Result.Failure<MfaVerifyResponse>(AuthResultCode.InvalidCredentials);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user, cancellationToken);

        var now = DateTime.UtcNow;
        var jwtOpts = jwtOptions.Value;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            TenantId = challenge.TenantId,
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
            TenantId = challenge.TenantId,
            SessionId = session.Id,
            TokenHash = refreshHash,
            ExpiresAt = session.ExpiresAt,
            Status = RefreshTokenStatus.Active,
            CreatedAt = now,
        };
        await refreshTokens.AddAsync(refresh, cancellationToken);

        var token = jwt.Issue(user, session.Id);

        return Result.Success(new MfaVerifyResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = refreshPlain,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            SessionId = session.Id,
            UserId = user.Id,
            TenantId = challenge.TenantId,
        });
    }

}
