using Microsoft.Extensions.DependencyInjection;

namespace Business.Libraries.Authentication.Sso;

public static class SsoExtensions
{
    /// <summary>
    /// Registers each supplied <see cref="ISsoProvider"/>. Each provider attaches its own
    /// authentication handler onto the shared <see cref="Microsoft.AspNetCore.Authentication.AuthenticationBuilder"/>
    /// and the framework remains independent of any specific OIDC/OAuth library.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddSHAuthentication(builder.Configuration, auth =&gt;
    /// {
    ///     auth.AddSso(
    ///         new GoogleSsoProvider(),
    ///         new EntraSsoProvider());
    /// });
    /// </code>
    /// </example>
    public static SHAuthenticationBuilder AddSso(this SHAuthenticationBuilder builder, params ISsoProvider[] providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var provider in providers)
        {
            ArgumentNullException.ThrowIfNull(provider);
            if (!seen.Add(provider.Scheme))
            {
                throw new InvalidOperationException(
                    $"Duplicate SSO scheme '{provider.Scheme}'. Each provider must have a unique scheme name.");
            }

            // Register the provider itself in DI so handlers / callbacks can resolve it later.
            builder.Services.AddSingleton(provider);
            provider.Configure(builder.AuthenticationBuilder, builder.Configuration);
        }

        return builder;
    }
}
