namespace Business.Caching;

/// <summary>
/// Provider-agnostic cache contract. Implementations live in <c>Caching.&lt;Name&gt;</c> projects
/// (e.g. <c>Caching.InMemory</c>, <c>Caching.Redis</c>). Consumers register one or more
/// implementations and inject either the interface directly (single-provider) or the
/// <c>IProviderFactory&lt;CacheCredential, ICacheProvider&gt;</c> for multi-provider routing.
/// </summary>
public interface ICacheProvider
{
    /// <summary>Fetches a value previously set under <paramref name="key"/>. Returns the type's default if absent.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Persists <paramref name="value"/> under <paramref name="key"/> with the supplied TTL, or
    /// <see cref="CacheOptions.DefaultTtl"/> when none is given.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>True when the key currently has a value (not expired, not removed).</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes the value. No-op if the key isn't present.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
