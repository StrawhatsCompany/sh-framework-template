using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Authentication.Mfa;

internal sealed class InMemoryMfaChallengeStore : IMfaChallengeStore
{
    private readonly ConcurrentDictionary<Guid, MfaChallenge> _byId = new();

    public Task<MfaChallenge> AddAsync(MfaChallenge challenge, CancellationToken ct = default)
    {
        _byId[challenge.Id] = challenge;
        return Task.FromResult(challenge);
    }

    public Task<MfaChallenge?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        _byId.TryGetValue(id, out var challenge);
        return Task.FromResult(challenge is not null && challenge.TenantId == tenantId ? challenge : null);
    }

    public Task<MfaChallenge?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        _byId.TryGetValue(id, out var challenge);
        return Task.FromResult<MfaChallenge?>(challenge);
    }

    public Task<MfaChallenge?> UpdateAsync(MfaChallenge challenge, CancellationToken ct = default)
    {
        if (!_byId.ContainsKey(challenge.Id)) return Task.FromResult<MfaChallenge?>(null);
        challenge.UpdatedAt = DateTime.UtcNow;
        _byId[challenge.Id] = challenge;
        return Task.FromResult<MfaChallenge?>(challenge);
    }
}
