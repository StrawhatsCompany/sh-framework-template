using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Authentication.Mfa;

internal sealed class InMemoryMfaFactorStore : IMfaFactorStore
{
    private readonly ConcurrentDictionary<Guid, MfaFactor> _byId = new();

    public Task<MfaFactor> AddAsync(MfaFactor factor, CancellationToken ct = default)
    {
        _byId[factor.Id] = factor;
        return Task.FromResult(factor);
    }

    public Task<MfaFactor?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        _byId.TryGetValue(id, out var factor);
        return Task.FromResult(factor is { DeletedAt: null } && factor.TenantId == tenantId ? factor : null);
    }

    public Task<IReadOnlyList<MfaFactor>> ListByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<MfaFactor> snapshot = _byId.Values
            .Where(f => f.TenantId == tenantId && f.UserId == userId && f.DeletedAt is null)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<MfaFactor>> ListActiveByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<MfaFactor> snapshot = _byId.Values
            .Where(f => f.TenantId == tenantId && f.UserId == userId
                        && f.DeletedAt is null && f.Status == MfaFactorStatus.Active)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<MfaFactor?> UpdateAsync(MfaFactor factor, CancellationToken ct = default)
    {
        if (!_byId.ContainsKey(factor.Id)) return Task.FromResult<MfaFactor?>(null);
        factor.UpdatedAt = DateTime.UtcNow;
        _byId[factor.Id] = factor;
        return Task.FromResult<MfaFactor?>(factor);
    }

    public Task<bool> SoftDeleteAsync(Guid tenantId, Guid id, Guid? deletedBy, CancellationToken ct = default)
    {
        if (!_byId.TryGetValue(id, out var factor)
            || factor.TenantId != tenantId
            || factor.DeletedAt is not null)
        {
            return Task.FromResult(false);
        }
        factor.DeletedAt = DateTime.UtcNow;
        factor.DeletedBy = deletedBy;
        factor.Status = MfaFactorStatus.Disabled;
        return Task.FromResult(true);
    }
}
