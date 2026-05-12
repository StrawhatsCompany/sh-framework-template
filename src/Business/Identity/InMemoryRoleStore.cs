using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Identity;

internal sealed class InMemoryRoleStore(IPermissionStore permissions) : IRoleStore
{
    private readonly ConcurrentDictionary<Guid, Role> _roles = new();
    private readonly ConcurrentDictionary<Guid, RolePermission> _rolePermissions = new();

    public Task<IReadOnlyList<Role>> ListAsync(Guid tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<Role> snapshot = _roles.Values
            .Where(r => r.TenantId == tenantId && r.DeletedAt is null)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<Role?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        _roles.TryGetValue(id, out var role);
        return Task.FromResult(role is { DeletedAt: null } && role.TenantId == tenantId ? role : null);
    }

    public Task<Role?> FindByNameAsync(Guid tenantId, string name, CancellationToken ct = default)
    {
        var match = _roles.Values.FirstOrDefault(r =>
            r.TenantId == tenantId && r.DeletedAt is null &&
            string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        _roles[role.Id] = role;
        return Task.FromResult(role);
    }

    public Task<Role?> UpdateAsync(Role role, CancellationToken ct = default)
    {
        if (!_roles.ContainsKey(role.Id) || _roles[role.Id].DeletedAt is not null)
        {
            return Task.FromResult<Role?>(null);
        }
        _roles[role.Id] = role;
        return Task.FromResult<Role?>(role);
    }

    public Task<bool> SoftDeleteAsync(Guid tenantId, Guid id, Guid? deletedBy, CancellationToken ct = default)
    {
        if (!_roles.TryGetValue(id, out var role) || role.TenantId != tenantId || role.DeletedAt is not null)
        {
            return Task.FromResult(false);
        }
        if (role.IsSystem)
        {
            return Task.FromResult(false);
        }
        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = deletedBy;
        return Task.FromResult(true);
    }

    public async Task<IReadOnlyList<Permission>> ListPermissionsAsync(Guid tenantId, Guid roleId, CancellationToken ct = default)
    {
        var permIds = _rolePermissions.Values
            .Where(rp => rp.TenantId == tenantId && rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        var allPerms = await permissions.ListAsync(ct);
        return allPerms.Where(p => permIds.Contains(p.Id)).ToList();
    }

    public Task SetPermissionsAsync(Guid tenantId, Guid roleId, IReadOnlyCollection<Guid> permissionIds, Guid? actingUserId, CancellationToken ct = default)
    {
        var existing = _rolePermissions.Values
            .Where(rp => rp.TenantId == tenantId && rp.RoleId == roleId)
            .ToList();
        foreach (var rp in existing)
        {
            _rolePermissions.TryRemove(rp.Id, out _);
        }

        var now = DateTime.UtcNow;
        foreach (var permId in permissionIds.Distinct())
        {
            var join = new RolePermission
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoleId = roleId,
                PermissionId = permId,
                CreatedAt = now,
                CreatedBy = actingUserId,
            };
            _rolePermissions[join.Id] = join;
        }
        return Task.CompletedTask;
    }
}
