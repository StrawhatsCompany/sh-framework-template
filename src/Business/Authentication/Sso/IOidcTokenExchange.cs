using Domain.Entities.Identity;

namespace Business.Authentication.Sso;

public interface IOidcTokenExchange
{
    /// <summary>
    /// Exchanges an authorization code for an id_token + access_token at the provider's
    /// token endpoint. Returns the raw id_token plus the decoded claims dictionary after
    /// signature + iss + aud + exp + nonce validation.
    /// </summary>
    Task<OidcExchangeResult> ExchangeAsync(
        SsoProvider provider,
        string clientSecret,
        string code,
        string codeVerifier,
        string redirectUri,
        string expectedNonce,
        CancellationToken ct = default);
}

public sealed record OidcExchangeResult(
    bool IsSuccess,
    string? ErrorDescription,
    string? IdTokenRaw,
    IReadOnlyDictionary<string, object>? Claims);
