using Domain.Entities.Identity;

namespace Business.Authentication.ApiKeys;

public interface IApiKeyStore
{
    Task<ApiKey> AddAsync(ApiKey apiKey, CancellationToken ct = default);
    Task<ApiKey?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> ListByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> ListByTenantAsync(Guid tenantId, Guid? userId = null, CancellationToken ct = default);

    /// <summary>
    /// Looks up by the random prefix segment (the 8 chars between the <c>shf_</c> and the
    /// secret). Indexed lookup in any backing store — does NOT scan all rows.
    /// </summary>
    Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken ct = default);

    Task<ApiKey?> UpdateAsync(ApiKey apiKey, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid tenantId, Guid id, Guid? revokedBy, CancellationToken ct = default);
}
