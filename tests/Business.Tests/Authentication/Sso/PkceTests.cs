using Business.Authentication.Sso;

namespace Business.Tests.Authentication.Sso;

public class PkceTests
{
    [Fact]
    public void CreateVerifier_returns_url_safe_string()
    {
        var verifier = Pkce.CreateVerifier();

        Assert.False(string.IsNullOrEmpty(verifier));
        Assert.DoesNotContain('+', verifier);
        Assert.DoesNotContain('/', verifier);
        Assert.DoesNotContain('=', verifier);
    }

    [Fact]
    public void ComputeChallenge_is_stable_for_same_verifier()
    {
        var verifier = Pkce.CreateVerifier();
        Assert.Equal(Pkce.ComputeChallenge(verifier), Pkce.ComputeChallenge(verifier));
    }

    [Fact]
    public void ComputeChallenge_differs_for_different_verifiers()
    {
        Assert.NotEqual(Pkce.ComputeChallenge(Pkce.CreateVerifier()),
                        Pkce.ComputeChallenge(Pkce.CreateVerifier()));
    }

    [Fact]
    public void CreateState_and_CreateNonce_return_unique_values_each_call()
    {
        Assert.NotEqual(Pkce.CreateState(), Pkce.CreateState());
        Assert.NotEqual(Pkce.CreateNonce(), Pkce.CreateNonce());
    }
}
