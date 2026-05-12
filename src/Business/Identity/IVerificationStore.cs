using Domain.Entities.Identity;

namespace Business.Identity;

public interface IVerificationStore
{
    Task<Verification> AddAsync(Verification verification, CancellationToken ct = default);
    Task<Verification?> FindActiveAsync(Guid tenantId, Guid userId, VerificationChannel channel, CancellationToken ct = default);
    Task<Verification?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task ConsumeAsync(Guid id, CancellationToken ct = default);
}
