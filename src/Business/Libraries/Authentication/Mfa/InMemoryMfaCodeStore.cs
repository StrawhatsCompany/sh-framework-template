using System.Collections.Concurrent;

namespace Business.Libraries.Authentication.Mfa;

/// <summary>
/// Default fallback store so <see cref="MfaChallengeIssuer"/> resolves cleanly in DI when the
/// consumer hasn't wired their own. Suitable for tests and single-instance dev; **never** use
/// this in production — challenges are lost on app restart and don't survive across instances.
/// Register a Redis / EF Core / SQL implementation via <c>TryAddSingleton&lt;IMfaCodeStore&gt;</c>.
/// </summary>
internal sealed class InMemoryMfaCodeStore : IMfaCodeStore
{
    private readonly ConcurrentDictionary<string, MfaChallenge> _items = new();

    public Task StoreAsync(MfaChallenge challenge, CancellationToken cancellationToken = default)
    {
        _items[challenge.ChallengeId] = challenge;
        return Task.CompletedTask;
    }

    public Task<MfaChallenge?> GetAsync(string challengeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.TryGetValue(challengeId, out var value) ? value : null);

    public Task UpdateAsync(MfaChallenge challenge, CancellationToken cancellationToken = default)
    {
        _items[challenge.ChallengeId] = challenge;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string challengeId, CancellationToken cancellationToken = default)
    {
        _items.TryRemove(challengeId, out _);
        return Task.CompletedTask;
    }
}
