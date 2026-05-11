namespace Business.Caching;

public enum CacheProviderType
{
    InMemory = 0,
    Redis = 1,
    Memcached = 2,
    DistributedSqlServer = 3,
}
