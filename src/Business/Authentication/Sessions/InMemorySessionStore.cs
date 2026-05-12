using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Authentication.Sessions;

internal sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public Task<Session> AddAsync(Session session, CancellationToken ct = default)
    {
        _sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task<Session?> FindByIdAsync(Guid tenantId, Guid sessionId, CancellationToken ct = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session is not null && session.TenantId == tenantId ? session : null);
    }

    public Task<IReadOnlyList<Session>> ListActiveByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<Session> snapshot = _sessions.Values
            .Where(s => s.TenantId == tenantId && s.UserId == userId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.LastSeenAt)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<Session?> UpdateAsync(Session session, CancellationToken ct = default)
    {
        if (!_sessions.ContainsKey(session.Id))
        {
            return Task.FromResult<Session?>(null);
        }
        _sessions[session.Id] = session;
        return Task.FromResult<Session?>(session);
    }

    public Task<bool> RevokeAsync(Guid tenantId, Guid sessionId, string reason, CancellationToken ct = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)
            || session.TenantId != tenantId
            || session.Status != SessionStatus.Active)
        {
            return Task.FromResult(false);
        }
        session.Status = SessionStatus.Revoked;
        session.RevokedAt = DateTime.UtcNow;
        session.RevokedReason = reason;
        return Task.FromResult(true);
    }

    public Task<int> RevokeAllForUserAsync(Guid tenantId, Guid userId, string reason, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var count = 0;
        foreach (var session in _sessions.Values
                     .Where(s => s.TenantId == tenantId && s.UserId == userId && s.Status == SessionStatus.Active))
        {
            session.Status = SessionStatus.Revoked;
            session.RevokedAt = now;
            session.RevokedReason = reason;
            count++;
        }
        return Task.FromResult(count);
    }
}
