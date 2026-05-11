using Business.Caching;
using Business.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.InMemory;

/// <summary>
/// Factory used when consumers need multi-provider routing (e.g., different caches for session
/// vs billing). For the single-provider case, just inject <see cref="ICacheProvider"/> directly
/// — the registration in <c>RegisterInMemoryCaching.AddInMemoryCaching</c> wires that path too.
/// </summary>
internal sealed class ProviderFactory(IServiceProvider services) : IProviderFactory<CacheCredential, ICacheProvider>
{
    public ICacheProvider Create(CacheCredential credential) =>
        credential.ProviderType switch
        {
            CacheProviderType.InMemory => services.GetRequiredService<InMemoryCacheProvider>(),
            _ => throw new NotSupportedException($"{credential.ProviderType} is not supported from Caching.InMemory"),
        };
}
