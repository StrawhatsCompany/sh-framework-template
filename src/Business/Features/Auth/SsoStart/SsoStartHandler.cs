using System.Web;
using Business.Authentication.Sso;
using Business.Common;
using Business.Configuration;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.SsoStart;

public sealed class SsoStartCommand : Request<SsoStartResponse>
{
    public string ProviderName { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public sealed class SsoStartResponse
{
    public required string AuthorizationUri { get; init; }
    public required string EncryptedStateCookie { get; init; }
    public required string CookieName { get; init; }
    public required DateTime CookieExpiresAt { get; init; }
}

public sealed class SsoStartHandler(
    ISsoProviderStore store,
    ICredentialProtector protector,
    ITenantContext tenantCtx,
    IOptionsSnapshot<SsoOptions> ssoOptions)
    : RequestHandler<SsoStartCommand, SsoStartResponse>
{
    public override async Task<Result<SsoStartResponse>> HandleAsync(
        SsoStartCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<SsoStartResponse>(SsoResultCode.ProviderNotFound);
        }

        var provider = await store.FindByNameAsync(tenantId, request.ProviderName, cancellationToken);
        if (provider is null)
        {
            return Result.Failure<SsoStartResponse>(SsoResultCode.ProviderNotFound);
        }
        if (provider.Status != SsoProviderStatus.Active)
        {
            return Result.Failure<SsoStartResponse>(SsoResultCode.ProviderDisabled);
        }

        var opts = ssoOptions.Value;

        // Validate returnUrl against the allowlist (open-redirect defence).
        if (!string.IsNullOrEmpty(request.ReturnUrl) && !ReturnUrlAllowed(request.ReturnUrl, opts.AllowedReturnUrls))
        {
            return Result.Failure<SsoStartResponse>(SsoResultCode.ReturnUrlNotAllowed);
        }

        var state = Pkce.CreateState();
        var nonce = Pkce.CreateNonce();
        var verifier = Pkce.CreateVerifier();
        var challenge = Pkce.ComputeChallenge(verifier);

        var cookiePayload = new SsoStateCookie(
            State: state,
            Nonce: nonce,
            CodeVerifier: verifier,
            ProviderId: provider.Id,
            TenantId: tenantId,
            ReturnUrl: request.ReturnUrl,
            ExpiresAt: DateTime.UtcNow.Add(opts.StateCookieTtl));
        var cookieRaw = SsoStateCookie.Serialize(cookiePayload);
        var encryptedCookie = protector.Protect(cookieRaw);

        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = provider.ClientId,
            ["redirect_uri"] = BuildRedirectUri(provider.Name),
            ["scope"] = provider.Scopes,
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
        };
        var qs = string.Join('&', query.Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}"));
        var sep = provider.AuthorizationEndpoint.Contains('?') ? '&' : '?';
        var uri = $"{provider.AuthorizationEndpoint}{sep}{qs}";

        return Result.Success(new SsoStartResponse
        {
            AuthorizationUri = uri,
            EncryptedStateCookie = encryptedCookie,
            CookieName = opts.StateCookieName,
            CookieExpiresAt = cookiePayload.ExpiresAt,
        });
    }

    // The actual redirect URI is the public callback the IdP will redirect back to. Endpoints
    // populate this with HttpContext.Request scheme + host so it matches what's configured on
    // the IdP side; this handler returns a relative placeholder which the endpoint replaces.
    private static string BuildRedirectUri(string providerName) =>
        $"/api/v1/auth/sso/{providerName}/callback";

    private static bool ReturnUrlAllowed(string returnUrl, string allowedCsv)
    {
        if (string.IsNullOrWhiteSpace(allowedCsv)) return false;
        return allowedCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(prefix => returnUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
