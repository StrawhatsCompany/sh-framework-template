using Microsoft.AspNetCore.Authentication;

namespace Business.Authentication.ApiKeys;

public static class ApiKeyAuthenticationExtensions
{
    /// <summary>
    /// Registers the ApiKey authentication scheme. Use alongside <c>AddJwtBearer</c> so endpoints
    /// can accept either an <c>Authorization: Bearer ...</c> JWT or an <c>Authorization: ApiKey ...</c>
    /// header.
    /// </summary>
    public static AuthenticationBuilder AddApiKeyAuthentication(
        this AuthenticationBuilder builder,
        Action<ApiKeyOptions>? configure = null) =>
        builder.AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationDefaults.AuthenticationScheme,
            opts => configure?.Invoke(opts));
}
