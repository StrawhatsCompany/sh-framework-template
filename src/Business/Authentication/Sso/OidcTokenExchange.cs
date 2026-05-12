using System.Net.Http.Json;
using System.Text.Json;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Business.Authentication.Sso;

internal sealed class OidcTokenExchange(
    IHttpClientFactory httpFactory,
    IOptionsSnapshot<SsoOptions> ssoOptions)
    : IOidcTokenExchange
{
    public async Task<OidcExchangeResult> ExchangeAsync(
        SsoProvider provider,
        string clientSecret,
        string code,
        string codeVerifier,
        string redirectUri,
        string expectedNonce,
        CancellationToken ct = default)
    {
        var http = httpFactory.CreateClient("Sso");

        // 1. Token exchange — POST application/x-www-form-urlencoded.
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = provider.ClientId,
            ["client_secret"] = clientSecret,
            ["code_verifier"] = codeVerifier,
        };
        using var tokenResp = await http.PostAsync(provider.TokenEndpoint, new FormUrlEncodedContent(form), ct);
        if (!tokenResp.IsSuccessStatusCode)
        {
            var body = await tokenResp.Content.ReadAsStringAsync(ct);
            return new OidcExchangeResult(false, $"Token endpoint returned {(int)tokenResp.StatusCode}: {body}", null, null);
        }

        var tokenPayload = await tokenResp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        if (tokenPayload is null || string.IsNullOrEmpty(tokenPayload.IdToken))
        {
            return new OidcExchangeResult(false, "Token endpoint response missing id_token", null, null);
        }

        // 2. Fetch JWKS from the provider's jwks_uri (for signature verification).
        if (string.IsNullOrEmpty(provider.JwksUri))
        {
            return new OidcExchangeResult(false, "SSO provider has no JwksUri configured", null, null);
        }

        JsonWebKeySet jwks;
        try
        {
            var jwksJson = await http.GetStringAsync(provider.JwksUri, ct);
            jwks = new JsonWebKeySet(jwksJson);
        }
        catch (Exception ex)
        {
            return new OidcExchangeResult(false, $"JWKS fetch failed: {ex.Message}", null, null);
        }

        // 3. Validate the id_token — signature + iss + aud + exp + nonce.
        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = provider.Issuer,
            ValidateAudience = true,
            ValidAudience = provider.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = jwks.Keys,
            ClockSkew = ssoOptions.Value.ClockSkew,
        };
        var handler = new JsonWebTokenHandler();
        var validation_result = await handler.ValidateTokenAsync(tokenPayload.IdToken, validation);
        if (!validation_result.IsValid)
        {
            return new OidcExchangeResult(false, $"id_token invalid: {validation_result.Exception?.Message}", null, null);
        }

        // 4. Nonce match.
        if (!validation_result.Claims.TryGetValue("nonce", out var nonce) ||
            !string.Equals(nonce?.ToString(), expectedNonce, StringComparison.Ordinal))
        {
            return new OidcExchangeResult(false, "Nonce mismatch", null, null);
        }

        return new OidcExchangeResult(true, null, tokenPayload.IdToken,
            new Dictionary<string, object>(validation_result.Claims));
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("id_token")] string? IdToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string? TokenType,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int? ExpiresIn);
}
