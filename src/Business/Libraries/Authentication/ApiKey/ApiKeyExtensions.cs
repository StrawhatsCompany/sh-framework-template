using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Business.Libraries.Authentication.ApiKey;

public static class ApiKeyExtensions
{
    /// <summary>
    /// Adds the API key authentication scheme. Reads keys from the <see cref="ApiKeyOptions.HeaderName"/>
    /// header (default <c>X-Api-Key</c>). The consumer must register an <see cref="IApiKeyValidator"/>
    /// implementation — by default a no-op validator that fails every request is registered so the
    /// app boots and the failure mode is obvious if no real validator was wired.
    /// </summary>
    public static SHAuthenticationBuilder AddApiKey(
        this SHAuthenticationBuilder builder,
        Action<ApiKeyOptions>? configureOptions = null)
    {
        builder.Services.TryAddSingleton<IApiKeyValidator, NoOpApiKeyValidator>();

        builder.AuthenticationBuilder.AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(
            ApiKeyDefaults.AuthenticationScheme,
            configureOptions);

        return builder;
    }

    private sealed class NoOpApiKeyValidator : IApiKeyValidator
    {
        // Default fail-closed implementation. Consumers register their own
        // `IApiKeyValidator` (DB lookup, env var, HMAC verification) over this.
        public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApiKeyValidationResult.Invalid);
    }
}
