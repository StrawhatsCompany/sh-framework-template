using Domain.Entities.Identity;

namespace Business.Identity;

public interface IUserStore
{
    Task<IReadOnlyList<User>> ListAsync(Guid tenantId, UserStatus? status = null, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<User?> FindByUsernameAsync(Guid tenantId, string username, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task<User?> UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid tenantId, Guid id, Guid? deletedBy, CancellationToken ct = default);

    Task<IReadOnlyList<Role>> ListRolesAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task SetRolesAsync(Guid tenantId, Guid userId, IReadOnlyCollection<Guid> roleIds, Guid? actingUserId, CancellationToken ct = default);
}
