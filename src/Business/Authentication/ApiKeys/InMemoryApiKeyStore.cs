using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Authentication.ApiKeys;

internal sealed class InMemoryApiKeyStore : IApiKeyStore
{
    private readonly ConcurrentDictionary<Guid, ApiKey> _byId = new();

    public Task<ApiKey> AddAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        _byId[apiKey.Id] = apiKey;
        return Task.FromResult(apiKey);
    }

    public Task<ApiKey?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        _byId.TryGetValue(id, out var apiKey);
        return Task.FromResult(apiKey is { DeletedAt: null } && apiKey.TenantId == tenantId ? apiKey : null);
    }

    public Task<IReadOnlyList<ApiKey>> ListByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<ApiKey> snapshot = _byId.Values
            .Where(k => k.TenantId == tenantId && k.UserId == userId && k.DeletedAt is null)
            .OrderByDescending(k => k.CreatedAt)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<ApiKey>> ListByTenantAsync(Guid tenantId, Guid? userId = null, CancellationToken ct = default)
    {
        IReadOnlyList<ApiKey> snapshot = _byId.Values
            .Where(k => k.TenantId == tenantId && k.DeletedAt is null)
            .Where(k => userId is null || k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // In persistence-backed stores this is a unique index on Prefix. In-memory scans.
        var match = _byId.Values.FirstOrDefault(k => k.Prefix == prefix && k.DeletedAt is null);
        return Task.FromResult(match);
    }

    public Task<ApiKey?> UpdateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        if (!_byId.ContainsKey(apiKey.Id))
        {
            return Task.FromResult<ApiKey?>(null);
        }
        apiKey.UpdatedAt = DateTime.UtcNow;
        _byId[apiKey.Id] = apiKey;
        return Task.FromResult<ApiKey?>(apiKey);
    }

    public Task<bool> RevokeAsync(Guid tenantId, Guid id, Guid? revokedBy, CancellationToken ct = default)
    {
        if (!_byId.TryGetValue(id, out var apiKey)
            || apiKey.TenantId != tenantId
            || apiKey.DeletedAt is not null
            || apiKey.Status == ApiKeyStatus.Revoked)
        {
            return Task.FromResult(false);
        }
        apiKey.Status = ApiKeyStatus.Revoked;
        apiKey.UpdatedAt = DateTime.UtcNow;
        apiKey.UpdatedBy = revokedBy;
        return Task.FromResult(true);
    }
}
