using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Libraries.Authentication;

public static class RegisterAuthentication
{
    /// <summary>
    /// Top-level entry point for wiring authentication. Caller chains schemes via the builder:
    /// <code>
    /// builder.Services.AddAuth(builder.Configuration, auth =&gt;
    /// {
    ///     auth.AddJwt();
    ///     // future: auth.AddApiKey(); auth.AddSso(); auth.AddMfa();
    /// });
    /// </code>
    /// </summary>
    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuthBuilder>? configure = null)
    {
        var authBuilder = services.AddAuthentication();
        services.AddAuthorization();

        var builder = new AuthBuilder(services, configuration, authBuilder);
        configure?.Invoke(builder);
        return services;
    }
}
