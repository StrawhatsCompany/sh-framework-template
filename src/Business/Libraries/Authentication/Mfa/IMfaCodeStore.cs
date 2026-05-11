namespace Business.Libraries.Authentication.Mfa;

/// <summary>
/// Persists pending MFA challenges. Consumer picks the backing store — in-memory for dev,
/// Redis for production scale, an EF Core table for durable storage with audit. Implementations
/// must be safe for concurrent access from multiple requests.
/// </summary>
public interface IMfaCodeStore
{
    Task StoreAsync(MfaChallenge challenge, CancellationToken cancellationToken = default);
    Task<MfaChallenge?> GetAsync(string challengeId, CancellationToken cancellationToken = default);
    Task UpdateAsync(MfaChallenge challenge, CancellationToken cancellationToken = default);
    Task RemoveAsync(string challengeId, CancellationToken cancellationToken = default);
}
