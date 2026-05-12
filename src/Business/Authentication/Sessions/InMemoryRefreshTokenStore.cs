using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Authentication.Sessions;

internal sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<Guid, RefreshToken> _tokens = new();

    public Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        _tokens[token.Id] = token;
        return Task.FromResult(token);
    }

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var match = _tokens.Values.FirstOrDefault(t => t.TokenHash == tokenHash);
        return Task.FromResult(match);
    }

    public Task<RefreshToken?> UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        if (!_tokens.ContainsKey(token.Id))
        {
            return Task.FromResult<RefreshToken?>(null);
        }
        token.UpdatedAt = DateTime.UtcNow;
        _tokens[token.Id] = token;
        return Task.FromResult<RefreshToken?>(token);
    }

    public Task RevokeFamilyAsync(Guid refreshTokenId, CancellationToken ct = default)
    {
        if (!_tokens.TryGetValue(refreshTokenId, out var seed)) return Task.CompletedTask;

        var family = new HashSet<Guid>();
        Walk(seed.Id, family);

        var now = DateTime.UtcNow;
        foreach (var id in family)
        {
            if (_tokens.TryGetValue(id, out var t) && t.Status != RefreshTokenStatus.Revoked)
            {
                t.Status = RefreshTokenStatus.Revoked;
                t.UpdatedAt = now;
            }
        }
        return Task.CompletedTask;
    }

    public Task RevokeAllForSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var t in _tokens.Values.Where(t => t.SessionId == sessionId && t.Status != RefreshTokenStatus.Revoked))
        {
            t.Status = RefreshTokenStatus.Revoked;
            t.UpdatedAt = now;
        }
        return Task.CompletedTask;
    }

    private void Walk(Guid id, HashSet<Guid> visited)
    {
        if (!visited.Add(id)) return;
        if (!_tokens.TryGetValue(id, out var node)) return;

        // Forward: this token's successor.
        if (node.ReplacedById is { } next) Walk(next, visited);

        // Backward: anyone whose ReplacedById points at this token.
        foreach (var parent in _tokens.Values.Where(t => t.ReplacedById == id))
        {
            Walk(parent.Id, visited);
        }
    }
}
