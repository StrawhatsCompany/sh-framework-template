using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Business.Identity;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Business.Authentication.ApiKeys;

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyStore apiKeys,
    IApiKeyFactory factory,
    IUserStore users)
    : AuthenticationHandler<ApiKeyOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var header = headerValues.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith(ApiKeyAuthenticationDefaults.HeaderPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header[ApiKeyAuthenticationDefaults.HeaderPrefix.Length..].Trim();
        if (!factory.TryParse(token, out var prefix, out _))
        {
            return AuthenticateResult.Fail("Invalid API key format.");
        }

        var stored = await apiKeys.FindByPrefixAsync(prefix, Context.RequestAborted);
        if (stored is null)
        {
            return AuthenticateResult.Fail("Unknown API key.");
        }

        var presentedHash = factory.Hash(token);
        if (!FixedTimeEquals(presentedHash, stored.KeyHash))
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (stored.Status != ApiKeyStatus.Active)
        {
            return AuthenticateResult.Fail("API key is not active.");
        }
        if (stored.ExpiresAt is { } expiresAt && expiresAt <= DateTime.UtcNow)
        {
            stored.Status = ApiKeyStatus.Expired;
            await apiKeys.UpdateAsync(stored, Context.RequestAborted);
            return AuthenticateResult.Fail("API key has expired.");
        }

        var user = await users.FindByIdAsync(stored.TenantId, stored.UserId, Context.RequestAborted);
        if (user is null || user.Status != UserStatus.Active)
        {
            return AuthenticateResult.Fail("API key owner is not active.");
        }

        // Debounced LastUsedAt update — fire-and-forget; failures don't break the request.
        var now = DateTime.UtcNow;
        if (stored.LastUsedAt is null || now - stored.LastUsedAt > Options.LastUsedUpdateThrottle)
        {
            stored.LastUsedAt = now;
            stored.LastUsedIp = Context.Connection.RemoteIpAddress?.ToString();
            _ = apiKeys.UpdateAsync(stored, CancellationToken.None);
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("tid", stored.TenantId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("preferred_username", user.Username),
                new Claim("name", user.DisplayName),
                new Claim("apk", stored.Id.ToString()),
            ],
            authenticationType: ApiKeyAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private static bool FixedTimeEquals(string a, string b) =>
        a.Length == b.Length
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));
}
