using System.Security.Cryptography;
using System.Text;

namespace Business.Authentication.Sessions;

internal sealed class RefreshTokenFactory : IRefreshTokenFactory
{
    private const int RandomByteLength = 32;   // 256 bits of entropy

    public (string Plaintext, string Hash) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(RandomByteLength);
        var plaintext = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return (plaintext, Hash(plaintext));
    }

    public string Hash(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(bytes);
    }
}
