using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication.Mfa;

internal sealed class MfaOrchestrator(
    IMfaFactorStore factors,
    IMfaChallengeStore challenges,
    IEnumerable<IMfaChannel> channels,
    IOptionsSnapshot<MfaOptions> options)
    : IMfaOrchestrator
{
    private readonly Dictionary<MfaFactorKind, IMfaChannel> _byKind = channels.ToDictionary(c => c.Kind);

    public async Task<Result<MfaChallenge>> IssueAsync(Guid tenantId, Guid userId, Guid factorId, CancellationToken ct = default)
    {
        var factor = await factors.FindByIdAsync(tenantId, factorId, ct);
        if (factor is null || factor.UserId != userId)
        {
            return Result.Failure<MfaChallenge>(MfaResultCode.FactorNotFound);
        }
        if (factor.Status != MfaFactorStatus.Active)
        {
            return Result.Failure<MfaChallenge>(MfaResultCode.FactorNotActive);
        }
        if (!_byKind.TryGetValue(factor.Kind, out var channel))
        {
            return Result.Failure<MfaChallenge>(MfaResultCode.KindUnsupported);
        }

        var opts = options.Value;
        var challenge = new MfaChallenge
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            MfaFactorId = factor.Id,
            Kind = factor.Kind,
            ExpiresAt = DateTime.UtcNow.Add(opts.ChallengeLifetime),
            Status = MfaChallengeStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        var issue = await channel.IssueAsync(factor, challenge, ct);
        if (!issue.IsSuccess)
        {
            return Result.Failure<MfaChallenge>(ResultCode.Instance(issue.Code, issue.CategorizedCode, issue.Description));
        }

        await challenges.AddAsync(challenge, ct);
        return Result.Success(challenge);
    }

    public async Task<Result> VerifyAsync(Guid tenantId, Guid challengeId, string code, CancellationToken ct = default)
    {
        var challenge = await challenges.FindByIdAsync(tenantId, challengeId, ct);
        if (challenge is null)
        {
            return Result.Failure(MfaResultCode.ChallengeNotFound);
        }

        var now = DateTime.UtcNow;
        switch (challenge.Status)
        {
            case MfaChallengeStatus.Consumed:
                return Result.Failure(MfaResultCode.ChallengeAlreadyConsumed);
            case MfaChallengeStatus.Failed:
                return Result.Failure(MfaResultCode.ChallengeFailed);
            case MfaChallengeStatus.Expired:
                return Result.Failure(MfaResultCode.ChallengeExpired);
        }
        if (challenge.ExpiresAt <= now)
        {
            challenge.Status = MfaChallengeStatus.Expired;
            await challenges.UpdateAsync(challenge, ct);
            return Result.Failure(MfaResultCode.ChallengeExpired);
        }

        var factor = await factors.FindByIdAsync(tenantId, challenge.MfaFactorId, ct);
        if (factor is null || factor.Status != MfaFactorStatus.Active)
        {
            return Result.Failure(MfaResultCode.FactorNotActive);
        }
        if (!_byKind.TryGetValue(factor.Kind, out var channel))
        {
            return Result.Failure(MfaResultCode.KindUnsupported);
        }

        var verify = await channel.VerifyAsync(factor, challenge, code, ct);
        if (!verify.IsSuccess)
        {
            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= options.Value.MaxFailedAttempts)
            {
                challenge.Status = MfaChallengeStatus.Failed;
            }
            await challenges.UpdateAsync(challenge, ct);
            return verify;
        }

        challenge.Status = MfaChallengeStatus.Consumed;
        challenge.ConsumedAt = now;
        await challenges.UpdateAsync(challenge, ct);

        factor.LastUsedAt = now;
        await factors.UpdateAsync(factor, ct);
        return Result.Success();
    }
}
