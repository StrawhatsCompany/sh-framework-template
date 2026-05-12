using Domain.Entities.Identity;

namespace Business.Authentication.Sessions;

public interface ISessionStore
{
    Task<Session> AddAsync(Session session, CancellationToken ct = default);
    Task<Session?> FindByIdAsync(Guid tenantId, Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<Session>> ListActiveByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task<Session?> UpdateAsync(Session session, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid tenantId, Guid sessionId, string reason, CancellationToken ct = default);
    Task<int> RevokeAllForUserAsync(Guid tenantId, Guid userId, string reason, CancellationToken ct = default);
}
