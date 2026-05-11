using Business.Caching;
using Business.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.InMemory;

public static class RegisterInMemoryCaching
{
    /// <summary>
    /// Wires the InMemory cache provider as the application's <see cref="ICacheProvider"/> plus the
    /// <see cref="IProviderFactory{TCredential, TProvider}"/> equivalent for multi-provider scenarios.
    /// Binds <see cref="CacheOptions"/> from the <c>Caching</c> configuration section.
    /// </summary>
    public static IServiceCollection AddInMemoryCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<InMemoryCacheProvider>();
        services.AddSingleton<ICacheProvider>(sp => sp.GetRequiredService<InMemoryCacheProvider>());
        services.AddSingleton<IProviderFactory<CacheCredential, ICacheProvider>, ProviderFactory>();
        return services;
    }
}
