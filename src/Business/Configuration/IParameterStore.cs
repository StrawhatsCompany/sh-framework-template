using Domain.Entities.Configuration;

namespace Business.Configuration;

public interface IParameterStore
{
    Task<IReadOnlyList<Parameter>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Parameter?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<Parameter> SetAsync(string key, string value, string? module = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
