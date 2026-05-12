using Business.Authentication.ApiKeys;
using Business.Authentication.Authorization;
using Business.Authentication.Jwt;
using Business.Authentication.Mfa;
using Business.Authentication.Mfa.Totp;
using Business.Authentication.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Business.Authentication;

public static class RegisterAuthentication
{
    /// <summary>
    /// Registers JWT issuer, login options, and permission policy machinery. Distinct from
    /// ASP.NET Core's <c>AddAuthentication</c> (auth-scheme registration); call both.
    /// </summary>
    public static IServiceCollection AddSHAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<LoginOptions>(configuration.GetSection(LoginOptions.SectionName));
        services.TryAddScoped<IJwtTokenIssuer, JwtTokenIssuer>();

        // Session + refresh token stores — in-memory defaults; persistence-backed impls
        // override via explicit registration after AddBusiness.
        services.TryAddSingleton<ISessionStore, InMemorySessionStore>();
        services.TryAddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        services.TryAddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();

        // API key store + factory.
        services.TryAddSingleton<IApiKeyStore, InMemoryApiKeyStore>();
        services.TryAddSingleton<IApiKeyFactory, ApiKeyFactory>();
        services.Configure<ApiKeyOptions>(configuration.GetSection(ApiKeyOptions.SectionName));

        // MFA — stores, options, TOTP channel, orchestrator. Email + SMS channels register
        // themselves via #79 and #80; the orchestrator resolves by Kind so additional channels
        // plug in without changes here.
        services.Configure<MfaOptions>(configuration.GetSection(MfaOptions.SectionName));
        services.TryAddSingleton<IMfaFactorStore, InMemoryMfaFactorStore>();
        services.TryAddSingleton<IMfaChallengeStore, InMemoryMfaChallengeStore>();
        services.TryAddSingleton<ITotpService, TotpService>();
        services.AddScoped<IMfaChannel, TotpMfaChannel>();
        services.AddScoped<IMfaOrchestrator, MfaOrchestrator>();

        // Permission policy machinery — gates endpoints with [HasPermission("admin.users.write")].
        // Resolves against the DB at request time (user -> roles -> permissions) so role changes
        // take effect without waiting for token expiry.
        services.TryAddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
