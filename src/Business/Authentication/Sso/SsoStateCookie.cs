using System.Text.Json;

namespace Business.Authentication.Sso;

/// <summary>
/// Payload carried in the encrypted state cookie between <c>/sso/{name}/start</c> and
/// <c>/sso/{name}/callback</c>. Stores the PKCE verifier (so we can complete the token
/// exchange), the nonce (so we can verify the id_token), and the post-login redirect target.
/// </summary>
internal sealed record SsoStateCookie(
    string State,
    string Nonce,
    string CodeVerifier,
    Guid ProviderId,
    Guid TenantId,
    string? ReturnUrl,
    DateTime ExpiresAt)
{
    public static string Serialize(SsoStateCookie payload) => JsonSerializer.Serialize(payload);
    public static SsoStateCookie? TryDeserialize(string raw)
    {
        try { return JsonSerializer.Deserialize<SsoStateCookie>(raw); }
        catch { return null; }
    }
}
