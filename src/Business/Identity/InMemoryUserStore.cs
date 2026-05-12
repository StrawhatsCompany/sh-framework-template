using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Identity;

internal sealed class InMemoryUserStore(IRoleStore roles) : IUserStore
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentDictionary<Guid, UserRole> _userRoles = new();

    public Task<IReadOnlyList<User>> ListAsync(Guid tenantId, UserStatus? status = null, CancellationToken ct = default)
    {
        IReadOnlyList<User> snapshot = _users.Values
            .Where(u => u.TenantId == tenantId && u.DeletedAt is null)
            .Where(u => status is null || u.Status == status)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<User?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user is { DeletedAt: null } && user.TenantId == tenantId ? user : null);
    }

    public Task<User?> FindByEmailAsync(Guid tenantId, string email, CancellationToken ct = default)
    {
        var match = _users.Values.FirstOrDefault(u =>
            u.TenantId == tenantId && u.DeletedAt is null &&
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<User?> FindByUsernameAsync(Guid tenantId, string username, CancellationToken ct = default)
    {
        var match = _users.Values.FirstOrDefault(u =>
            u.TenantId == tenantId && u.DeletedAt is null &&
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<User?> UpdateAsync(User user, CancellationToken ct = default)
    {
        if (!_users.ContainsKey(user.Id) || _users[user.Id].DeletedAt is not null)
        {
            return Task.FromResult<User?>(null);
        }
        _users[user.Id] = user;
        return Task.FromResult<User?>(user);
    }

    public Task<bool> SoftDeleteAsync(Guid tenantId, Guid id, Guid? deletedBy, CancellationToken ct = default)
    {
        if (!_users.TryGetValue(id, out var user) || user.TenantId != tenantId || user.DeletedAt is not null)
        {
            return Task.FromResult(false);
        }
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = deletedBy;
        return Task.FromResult(true);
    }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var roleIds = _userRoles.Values
            .Where(ur => ur.TenantId == tenantId && ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToHashSet();

        var allRoles = await roles.ListAsync(tenantId, ct);
        return allRoles.Where(r => roleIds.Contains(r.Id)).ToList();
    }

    public Task SetRolesAsync(Guid tenantId, Guid userId, IReadOnlyCollection<Guid> roleIds, Guid? actingUserId, CancellationToken ct = default)
    {
        var existing = _userRoles.Values
            .Where(ur => ur.TenantId == tenantId && ur.UserId == userId)
            .ToList();
        foreach (var ur in existing)
        {
            _userRoles.TryRemove(ur.Id, out _);
        }

        var now = DateTime.UtcNow;
        foreach (var roleId in roleIds.Distinct())
        {
            var join = new UserRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                RoleId = roleId,
                CreatedAt = now,
                CreatedBy = actingUserId,
            };
            _userRoles[join.Id] = join;
        }
        return Task.CompletedTask;
    }
}
