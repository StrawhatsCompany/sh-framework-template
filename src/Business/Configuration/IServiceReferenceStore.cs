using Domain.Entities.Configuration;

namespace Business.Configuration;

public interface IServiceReferenceStore
{
    Task<IReadOnlyList<ServiceReference>> GetActiveAsync(string category, CancellationToken cancellationToken = default);
    Task<ServiceReference?> GetByGroupAsync(string category, string group, CancellationToken cancellationToken = default);
    Task<ServiceReference> AddAsync(ServiceReference reference, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
