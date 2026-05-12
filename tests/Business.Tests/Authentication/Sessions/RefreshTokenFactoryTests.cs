using Business.Authentication.Sessions;

namespace Business.Tests.Authentication.Sessions;

public class RefreshTokenFactoryTests
{
    private readonly RefreshTokenFactory _factory = new();

    [Fact]
    public void Generate_returns_unique_plaintext_and_matching_hash()
    {
        var (plain1, hash1) = _factory.Generate();
        var (plain2, hash2) = _factory.Generate();

        Assert.False(string.IsNullOrEmpty(plain1));
        Assert.NotEqual(plain1, plain2);
        Assert.NotEqual(hash1, hash2);
        Assert.Equal(hash1, _factory.Hash(plain1));
        Assert.Equal(hash2, _factory.Hash(plain2));
    }

    [Fact]
    public void Hash_is_url_safe_base64_so_hex_output()
    {
        var (_, hash) = _factory.Generate();

        // SHA-256 -> 32 bytes -> 64 hex chars
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9A-F]+$", hash);
    }
}
