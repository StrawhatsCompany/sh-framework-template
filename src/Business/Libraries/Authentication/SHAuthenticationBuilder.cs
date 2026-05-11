using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Libraries.Authentication;

/// <summary>
/// Fluent surface for wiring authentication schemes onto an <see cref="IServiceCollection"/>.
/// Held briefly during <c>AddSHAuthentication(...)</c> while consumers chain <c>AddJwt</c>,
/// <c>AddApiKey</c>, <c>AddSso</c>, <c>AddMfa</c>, etc. (Methods other than the JWT scaffolding
/// land in their own issues — see #44 / #45 / #46 / #47.)
/// </summary>
public sealed class SHAuthenticationBuilder
{
    internal SHAuthenticationBuilder(IServiceCollection services, IConfiguration configuration, AuthenticationBuilder authenticationBuilder)
    {
        Services = services;
        Configuration = configuration;
        AuthenticationBuilder = authenticationBuilder;
    }

    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }

    /// <summary>
    /// The underlying ASP.NET <see cref="AuthenticationBuilder"/>. Exposed so SSO providers and
    /// custom schemes can register their handlers without re-doing the bootstrap.
    /// </summary>
    public AuthenticationBuilder AuthenticationBuilder { get; }
}
