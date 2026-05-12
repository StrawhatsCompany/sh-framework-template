using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Business.Identity;

public static class RegisterIdentity
{
    /// <summary>
    /// Registers identity-domain stores + password hasher with their in-memory / default
    /// implementations. Persistence-backed stores (added via <c>shf make:persistence</c>)
    /// override these via explicit AddSingleton/AddScoped calls after AddBusiness.
    /// </summary>
    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services.TryAddSingleton<ITenantStore, InMemoryTenantStore>();
        services.TryAddSingleton<IPermissionStore, InMemoryPermissionStore>();
        services.TryAddSingleton<IRoleStore, InMemoryRoleStore>();
        services.TryAddSingleton<IUserStore, InMemoryUserStore>();
        services.TryAddSingleton<IVerificationStore, InMemoryVerificationStore>();
        services.TryAddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

        // Seeds the standard admin.* permission catalog on startup. Idempotent.
        services.AddHostedService<PermissionSeeder>();

        return services;
    }
}
