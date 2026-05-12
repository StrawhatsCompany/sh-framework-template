using System.Security.Cryptography;
using System.Text;

namespace Business.Authentication.ApiKeys;

internal sealed class ApiKeyFactory : IApiKeyFactory
{
    private const string TokenPrefix = "shf_";
    private const int PrefixLength = 8;
    private const int SecretLength = 32;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public GeneratedApiKey Generate()
    {
        var prefix = RandomString(PrefixLength);
        var secret = RandomString(SecretLength);
        var plaintext = $"{TokenPrefix}{prefix}_{secret}";
        var last4 = secret[^4..];
        return new GeneratedApiKey(plaintext, prefix, last4, Hash(plaintext));
    }

    public bool TryParse(string token, out string prefix, out string secret)
    {
        prefix = "";
        secret = "";
        if (string.IsNullOrEmpty(token) || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return false;
        }
        var rest = token[TokenPrefix.Length..];
        var underscore = rest.IndexOf('_');
        if (underscore != PrefixLength)
        {
            return false;
        }
        var maybePrefix = rest[..underscore];
        var maybeSecret = rest[(underscore + 1)..];
        if (maybePrefix.Length != PrefixLength
            || maybeSecret.Length != SecretLength
            || !maybePrefix.All(IsAlnum)
            || !maybeSecret.All(IsAlnum))
        {
            return false;
        }
        prefix = maybePrefix;
        secret = maybeSecret;
        return true;
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string RandomString(int length)
    {
        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return new string(result);
    }

    private static bool IsAlnum(char c) =>
        c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z';
}
