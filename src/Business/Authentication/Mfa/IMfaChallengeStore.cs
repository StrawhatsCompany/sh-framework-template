using Domain.Entities.Identity;

namespace Business.Authentication.Mfa;

public interface IMfaChallengeStore
{
    Task<MfaChallenge> AddAsync(MfaChallenge challenge, CancellationToken ct = default);
    Task<MfaChallenge?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cross-tenant lookup used by the pre-auth MFA verify endpoint — at challenge-presentation
    /// time the caller is unauthenticated, so there is no tenant context yet. Persistence-backed
    /// stores should index on Id (globally unique GUID).
    /// </summary>
    Task<MfaChallenge?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<MfaChallenge?> UpdateAsync(MfaChallenge challenge, CancellationToken ct = default);
}
