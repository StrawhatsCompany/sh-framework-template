using Business.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Caching.InMemory;

internal sealed class InMemoryCacheProvider(IMemoryCache cache, IOptions<CacheOptions> options) : ICacheProvider
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var prefixed = PrefixedKey(key);
        return Task.FromResult(cache.TryGetValue<T>(prefixed, out var value) ? value : default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var prefixed = PrefixedKey(key);
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? options.Value.DefaultTtl,
        };
        cache.Set(prefixed, value, entryOptions);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var prefixed = PrefixedKey(key);
        return Task.FromResult(cache.TryGetValue(prefixed, out _));
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cache.Remove(PrefixedKey(key));
        return Task.CompletedTask;
    }

    private string PrefixedKey(string key) =>
        string.IsNullOrEmpty(options.Value.KeyPrefix) ? key : $"{options.Value.KeyPrefix}:{key}";
}
