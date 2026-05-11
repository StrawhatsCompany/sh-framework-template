using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Business.Configuration;

public static class RegisterConfiguration
{
    public static IServiceCollection AddConfigurationStore(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.TryAddSingleton<ICredentialProtector, DataProtectionCredentialProtector>();
        services.TryAddSingleton<IServiceReferenceStore, InMemoryServiceReferenceStore>();
        return services;
    }
}
