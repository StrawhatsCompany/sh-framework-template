using Domain.Entities.Identity;

namespace Business.Authentication.Sso;

public interface ISsoProviderStore
{
    Task<SsoProvider> AddAsync(SsoProvider provider, CancellationToken ct = default);
    Task<SsoProvider?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<SsoProvider?> FindByNameAsync(Guid tenantId, string name, CancellationToken ct = default);
    Task<IReadOnlyList<SsoProvider>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task<SsoProvider?> UpdateAsync(SsoProvider provider, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid tenantId, Guid id, Guid? deletedBy, CancellationToken ct = default);
}
