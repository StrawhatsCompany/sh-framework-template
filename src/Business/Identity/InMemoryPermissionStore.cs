using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Identity;

internal sealed class InMemoryPermissionStore : IPermissionStore
{
    private readonly ConcurrentDictionary<Guid, Permission> _permissions = new();

    public Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Permission> snapshot = _permissions.Values
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<Permission?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        _permissions.TryGetValue(id, out var permission);
        return Task.FromResult(permission);
    }

    public Task<Permission?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        var match = _permissions.Values.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<Permission> AddAsync(Permission permission, CancellationToken ct = default)
    {
        _permissions[permission.Id] = permission;
        return Task.FromResult(permission);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(_permissions.TryRemove(id, out _));
    }
}
