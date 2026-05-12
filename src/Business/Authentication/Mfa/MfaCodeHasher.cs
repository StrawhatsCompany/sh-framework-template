using System.Security.Cryptography;
using System.Text;

namespace Business.Authentication.Mfa;

/// <summary>
/// Shared SHA-256 hashing for Email/SMS MFA codes (and any future dispatch-style channels).
/// TOTP doesn't use this — its codes are computed from the secret on demand and never stored.
/// </summary>
internal static class MfaCodeHasher
{
    public static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    public static string GenerateNumericCode(int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)('0' + bytes[i] % 10);
        }
        return new string(chars);
    }
}
