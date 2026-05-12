using System.Security.Cryptography;
using System.Text;
using Business.Authentication;
using Business.Authentication.Mfa;
using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.MfaSms;

public sealed class ConfirmSmsCommand : Request
{
    public Guid FactorId { get; set; }
    public Guid ChallengeId { get; set; }
    public string Code { get; set; } = "";
}

public sealed class ConfirmSmsHandler(
    IMfaFactorStore factors,
    IMfaChallengeStore challenges,
    IUserStore users,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<ConfirmSmsCommand>
{
    public override async Task<Result> HandleAsync(ConfirmSmsCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure(AuthResultCode.InvalidCredentials);
        }

        var factor = await factors.FindByIdAsync(tenantId, request.FactorId, cancellationToken);
        if (factor is null || factor.UserId != userId || factor.Kind != MfaFactorKind.Sms)
        {
            return Result.Failure(MfaResultCode.FactorNotFound);
        }
        if (factor.Status != MfaFactorStatus.PendingEnrollment)
        {
            return Result.Failure(MfaResultCode.FactorAlreadyEnrolled);
        }

        var challenge = await challenges.FindByIdAsync(tenantId, request.ChallengeId, cancellationToken);
        if (challenge is null || challenge.MfaFactorId != factor.Id || challenge.UserId != userId)
        {
            return Result.Failure(MfaResultCode.ChallengeNotFound);
        }
        if (challenge.Status != MfaChallengeStatus.Pending)
        {
            return Result.Failure(MfaResultCode.ChallengeAlreadyConsumed);
        }
        if (challenge.ExpiresAt <= DateTime.UtcNow)
        {
            challenge.Status = MfaChallengeStatus.Expired;
            await challenges.UpdateAsync(challenge, cancellationToken);
            return Result.Failure(MfaResultCode.ChallengeExpired);
        }
        if (string.IsNullOrEmpty(challenge.CodeHash))
        {
            return Result.Failure(MfaResultCode.InvalidCode);
        }

        var presentedHash = MfaCodeHasher.Hash(request.Code.Trim());
        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(presentedHash),
            Encoding.ASCII.GetBytes(challenge.CodeHash));
        if (!ok)
        {
            return Result.Failure(MfaResultCode.InvalidCode);
        }

        var now = DateTime.UtcNow;
        challenge.Status = MfaChallengeStatus.Consumed;
        challenge.ConsumedAt = now;
        await challenges.UpdateAsync(challenge, cancellationToken);

        factor.Status = MfaFactorStatus.Active;
        factor.VerifiedAt = now;
        factor.UpdatedBy = userId;
        await factors.UpdateAsync(factor, cancellationToken);

        // If the confirmed phone matches the user's primary phone, also stamp PhoneVerifiedAt.
        var user = await users.FindByIdAsync(tenantId, userId, cancellationToken);
        if (user is not null
            && string.Equals(user.Phone, factor.Destination, StringComparison.Ordinal)
            && user.PhoneVerifiedAt is null)
        {
            user.PhoneVerifiedAt = now;
            await users.UpdateAsync(user, cancellationToken);
        }

        return Result.Success();
    }
}
