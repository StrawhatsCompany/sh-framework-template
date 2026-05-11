namespace Business.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Caching";

    /// <summary>Default TTL applied when <c>SetAsync</c> is called without an explicit value.</summary>
    public TimeSpan DefaultTtl { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Prefix prepended to every key — keeps cache namespaces from colliding between services
    /// sharing the same backing store. Empty by default.</summary>
    public string KeyPrefix { get; init; } = string.Empty;
}
