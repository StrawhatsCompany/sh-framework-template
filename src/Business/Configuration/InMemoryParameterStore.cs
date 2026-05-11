using System.Collections.Concurrent;
using Domain.Entities.Configuration;

namespace Business.Configuration;

internal sealed class InMemoryParameterStore : IParameterStore
{
    private readonly ConcurrentDictionary<string, Parameter> _byKey = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<Parameter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Parameter> snapshot = _byKey.Values.ToList();
        return Task.FromResult(snapshot);
    }

    public Task<Parameter?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        _byKey.TryGetValue(key, out var parameter);
        return Task.FromResult(parameter);
    }

    public Task<Parameter> SetAsync(string key, string value, string? module = null, CancellationToken cancellationToken = default)
    {
        var parameter = new Parameter
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            Module = module,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _byKey[key] = parameter;
        return Task.FromResult(parameter);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _byKey.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
