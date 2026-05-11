using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace Business.Libraries.Authentication.Sso;

/// <summary>
/// Consumer-implemented contract describing one federated identity provider (Google, GitHub,
/// Microsoft Entra, etc.). The framework iterates all registered <see cref="ISsoProvider"/>
/// services from DI and lets each one register its own ASP.NET handler on the shared
/// <see cref="AuthenticationBuilder"/>. The framework deliberately has no dependency on any
/// specific OIDC/OAuth library — consumers bring exactly the providers they need.
/// </summary>
public interface ISsoProvider
{
    /// <summary>Unique scheme name (e.g. <c>"Google"</c>, <c>"GitHub"</c>, <c>"Entra"</c>).</summary>
    string Scheme { get; }

    /// <summary>
    /// Wires this provider's authentication handler onto <paramref name="builder"/>. Implementations
    /// typically call <c>builder.AddOpenIdConnect(Scheme, ...)</c> or <c>builder.AddOAuth(Scheme, ...)</c>
    /// and read their client-id / secret from <paramref name="configuration"/> (secrets via
    /// user-secrets / env vars).
    /// </summary>
    void Configure(AuthenticationBuilder builder, IConfiguration configuration);
}
