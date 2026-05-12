using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication.Mfa;

public interface IMfaOrchestrator
{
    /// <summary>
    /// Creates a new pending challenge for the user's <paramref name="factorId"/> and (for
    /// dispatch-style channels) sends the code via the channel. Returns the persisted challenge.
    /// </summary>
    Task<Result<MfaChallenge>> IssueAsync(Guid tenantId, Guid userId, Guid factorId, CancellationToken ct = default);

    /// <summary>
    /// Verifies the submitted code against the challenge. On success the challenge is marked
    /// Consumed and the factor's LastUsedAt is stamped. After
    /// <c>MfaOptions.MaxFailedAttempts</c> failures the challenge is marked Failed and a new
    /// challenge must be issued.
    /// </summary>
    Task<Result> VerifyAsync(Guid tenantId, Guid challengeId, string code, CancellationToken ct = default);
}
