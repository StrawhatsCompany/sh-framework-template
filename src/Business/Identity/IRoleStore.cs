using Domain.Entities.Identity;

namespace Business.Identity;

public interface IRoleStore
{
    Task<IReadOnlyList<Role>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task<Role?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<Role?> FindByNameAsync(Guid tenantId, string name, CancellationToken ct = default);
    Task<Role> AddAsync(Role role, CancellationToken ct = default);
    Task<Role?> UpdateAsync(Role role, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid tenantId, Guid id, Guid? deletedBy, CancellationToken ct = default);

    Task<IReadOnlyList<Permission>> ListPermissionsAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
    Task SetPermissionsAsync(Guid tenantId, Guid roleId, IReadOnlyCollection<Guid> permissionIds, Guid? actingUserId, CancellationToken ct = default);
}
