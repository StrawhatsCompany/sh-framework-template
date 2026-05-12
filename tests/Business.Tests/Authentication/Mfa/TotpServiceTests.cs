using Business.Authentication.Mfa.Totp;
using OtpNet;

namespace Business.Tests.Authentication.Mfa;

public class TotpServiceTests
{
    private readonly TotpService _service = new();

    [Fact]
    public void GenerateSecret_returns_base32_string_of_expected_length()
    {
        var secret = _service.GenerateSecret();

        Assert.False(string.IsNullOrEmpty(secret));
        // 20 random bytes encoded in base32 = 32 base32 chars
        Assert.Equal(32, secret.Length);
        // Round-trip — decode succeeds
        var bytes = Base32Encoding.ToBytes(secret);
        Assert.Equal(20, bytes.Length);
    }

    [Fact]
    public void Verify_accepts_freshly_computed_code()
    {
        var secret = _service.GenerateSecret();
        var bytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(bytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
        var current = totp.ComputeTotp(DateTime.UtcNow);

        Assert.True(_service.Verify(secret, current));
    }

    [Fact]
    public void Verify_rejects_wrong_code()
    {
        var secret = _service.GenerateSecret();
        Assert.False(_service.Verify(secret, "000000"));
    }

    [Fact]
    public void Verify_rejects_garbled_secret()
    {
        Assert.False(_service.Verify("not-base32", "123456"));
    }

    [Fact]
    public void BuildOtpAuthUri_includes_issuer_label_secret()
    {
        var uri = _service.BuildOtpAuthUri("SHFramework", "alice@example.com", "JBSWY3DPEHPK3PXP");

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains("SHFramework", uri);
        Assert.Contains("alice", uri);
        Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri);
        Assert.Contains("algorithm=SHA1", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }
}
