using System.Security.Cryptography;
using System.Text;

namespace Business.Authentication.Sso;

internal static class Pkce
{
    /// <summary>RFC 7636 S256 PKCE verifier — 32 random bytes, url-safe base64, no padding.</summary>
    public static string CreateVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return UrlSafe(bytes);
    }

    /// <summary>Computes the S256 challenge from a verifier: base64url(SHA-256(verifier)).</summary>
    public static string ComputeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return UrlSafe(hash);
    }

    /// <summary>Generates a 32-byte random nonce, url-safe base64 encoded.</summary>
    public static string CreateNonce() => UrlSafe(RandomNumberGenerator.GetBytes(32));

    /// <summary>Generates a 32-byte random state, url-safe base64 encoded.</summary>
    public static string CreateState() => UrlSafe(RandomNumberGenerator.GetBytes(32));

    private static string UrlSafe(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
