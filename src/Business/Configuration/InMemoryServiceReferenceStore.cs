using System.Collections.Concurrent;
using Domain.Entities.Configuration;

namespace Business.Configuration;

internal sealed class InMemoryServiceReferenceStore : IServiceReferenceStore
{
    private readonly ConcurrentDictionary<Guid, ServiceReference> _byId = new();

    public Task<IReadOnlyList<ServiceReference>> GetActiveAsync(string category, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ServiceReference> matches = _byId.Values
            .Where(r => r.IsActive && string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<ServiceReference?> GetByGroupAsync(string category, string group, CancellationToken cancellationToken = default)
    {
        var match = _byId.Values.FirstOrDefault(r =>
            r.IsActive
            && string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase)
            && string.Equals(r.Group, group, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<ServiceReference> AddAsync(ServiceReference reference, CancellationToken cancellationToken = default)
    {
        _byId[reference.Id] = reference;
        return Task.FromResult(reference);
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _byId.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
