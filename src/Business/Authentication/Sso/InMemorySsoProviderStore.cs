using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Authentication.Sso;

internal sealed class InMemorySsoProviderStore : ISsoProviderStore
{
    private readonly ConcurrentDictionary<Guid, SsoProvider> _byId = new();

    public Task<SsoProvider> AddAsync(SsoProvider provider, CancellationToken ct = default)
    {
        _byId[provider.Id] = provider;
        return Task.FromResult(provider);
    }

    public Task<SsoProvider?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        _byId.TryGetValue(id, out var p);
        return Task.FromResult(p is { DeletedAt: null } && p.TenantId == tenantId ? p : null);
    }

    public Task<SsoProvider?> FindByNameAsync(Guid tenantId, string name, CancellationToken ct = default)
    {
        var match = _byId.Values.FirstOrDefault(p =>
            p.TenantId == tenantId && p.DeletedAt is null &&
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<SsoProvider>> ListAsync(Guid tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<SsoProvider> snapshot = _byId.Values
            .Where(p => p.TenantId == tenantId && p.DeletedAt is null)
            .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<SsoProvider?> UpdateAsync(SsoProvider provider, CancellationToken ct = default)
    {
        if (!_byId.ContainsKey(provider.Id)) return Task.FromResult<SsoProvider?>(null);
        provider.UpdatedAt = DateTime.UtcNow;
        _byId[provider.Id] = provider;
        return Task.FromResult<SsoProvider?>(provider);
    }

    public Task<bool> SoftDeleteAsync(Guid tenantId, Guid id, Guid? deletedBy, CancellationToken ct = default)
    {
        if (!_byId.TryGetValue(id, out var p) || p.TenantId != tenantId || p.DeletedAt is not null)
        {
            return Task.FromResult(false);
        }
        p.DeletedAt = DateTime.UtcNow;
        p.DeletedBy = deletedBy;
        p.Status = SsoProviderStatus.Disabled;
        return Task.FromResult(true);
    }
}
