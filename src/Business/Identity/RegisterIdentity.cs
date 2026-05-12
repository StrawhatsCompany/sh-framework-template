using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Business.Identity;

public static class RegisterIdentity
{
    /// <summary>
    /// Registers identity-domain stores with their in-memory defaults. Persistence-backed
    /// implementations (added via <c>shf make:persistence</c>) override these via explicit
    /// AddSingleton/AddScoped calls after AddBusiness.
    /// </summary>
    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services.TryAddSingleton<ITenantStore, InMemoryTenantStore>();
        return services;
    }
}
