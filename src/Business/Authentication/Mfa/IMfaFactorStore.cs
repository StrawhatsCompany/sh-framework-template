using Domain.Entities.Identity;

namespace Business.Authentication.Mfa;

public interface IMfaFactorStore
{
    Task<MfaFactor> AddAsync(MfaFactor factor, CancellationToken ct = default);
    Task<MfaFactor?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MfaFactor>> ListByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<MfaFactor>> ListActiveByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task<MfaFactor?> UpdateAsync(MfaFactor factor, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid tenantId, Guid id, Guid? deletedBy, CancellationToken ct = default);
}
