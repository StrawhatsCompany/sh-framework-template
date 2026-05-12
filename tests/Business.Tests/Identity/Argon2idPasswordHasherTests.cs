using Business.Identity;

namespace Business.Tests.Identity;

public class Argon2idPasswordHasherTests
{
    private readonly Argon2idPasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_Verify_round_trip_succeeds()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.True(_hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_rejects_wrong_password()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.False(_hasher.Verify("Tr0ub4dor&3", hash));
    }

    [Fact]
    public void Hashes_are_salted_so_same_password_yields_different_hashes()
    {
        var a = _hasher.Hash("same");
        var b = _hasher.Hash("same");

        Assert.NotEqual(a, b);
        Assert.True(_hasher.Verify("same", a));
        Assert.True(_hasher.Verify("same", b));
    }

    [Fact]
    public void Verify_rejects_garbled_hash()
    {
        Assert.False(_hasher.Verify("any", ""));
        Assert.False(_hasher.Verify("any", "not-an-argon2-hash"));
        Assert.False(_hasher.Verify("any", "$argon2id$v=19$m=65536,t=3,p=4$abc"));
    }

    [Fact]
    public void Hash_encodes_parameters_in_output()
    {
        var hash = _hasher.Hash("x");

        Assert.StartsWith("$argon2id$v=19$m=65536,t=3,p=4$", hash);
        Assert.Equal(5, hash.Split('$', StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
