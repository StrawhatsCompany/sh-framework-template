using Domain.Entities.Identity;

namespace Business.Identity;

public interface IPermissionStore
{
    Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct = default);
    Task<Permission?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Permission?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<Permission> AddAsync(Permission permission, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
