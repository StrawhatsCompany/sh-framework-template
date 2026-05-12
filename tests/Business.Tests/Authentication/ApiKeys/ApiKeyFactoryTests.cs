using Business.Authentication.ApiKeys;

namespace Business.Tests.Authentication.ApiKeys;

public class ApiKeyFactoryTests
{
    private readonly ApiKeyFactory _factory = new();

    [Fact]
    public void Generate_returns_canonical_format()
    {
        var generated = _factory.Generate();

        Assert.StartsWith("shf_", generated.Plaintext);
        var parts = generated.Plaintext.Split('_');
        Assert.Equal(3, parts.Length);
        Assert.Equal("shf", parts[0]);
        Assert.Equal(8, parts[1].Length);
        Assert.Equal(32, parts[2].Length);

        Assert.Equal(generated.Prefix, parts[1]);
        Assert.Equal(generated.Last4, parts[2][^4..]);
        Assert.Equal(64, generated.KeyHash.Length); // SHA-256 hex
    }

    [Fact]
    public void Generate_returns_unique_tokens_each_call()
    {
        var a = _factory.Generate();
        var b = _factory.Generate();

        Assert.NotEqual(a.Plaintext, b.Plaintext);
        Assert.NotEqual(a.Prefix, b.Prefix);
        Assert.NotEqual(a.KeyHash, b.KeyHash);
    }

    [Theory]
    [InlineData("shf_a3f9b2c1_kP2sX9mQwL4rT6vN8jY5zF1bH3aD7cE0", true, "a3f9b2c1", "kP2sX9mQwL4rT6vN8jY5zF1bH3aD7cE0")]
    [InlineData("bearer_token", false, "", "")]
    [InlineData("shf_short_secret", false, "", "")]
    [InlineData("shf_a3f9b2c1_TOO_SHORT", false, "", "")]
    [InlineData("shf_a3f9b2c1!_kP2sX9mQwL4rT6vN8jY5zF1bH3aD7cE0", false, "", "")]
    [InlineData("", false, "", "")]
    public void TryParse_validates_format(string input, bool expectedSuccess, string expectedPrefix, string expectedSecret)
    {
        var ok = _factory.TryParse(input, out var prefix, out var secret);

        Assert.Equal(expectedSuccess, ok);
        Assert.Equal(expectedPrefix, prefix);
        Assert.Equal(expectedSecret, secret);
    }

    [Fact]
    public void Hash_is_stable_for_same_input()
    {
        var token = _factory.Generate().Plaintext;
        Assert.Equal(_factory.Hash(token), _factory.Hash(token));
    }
}
