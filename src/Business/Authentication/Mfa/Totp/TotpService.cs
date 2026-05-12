using System.Web;
using OtpNet;

namespace Business.Authentication.Mfa.Totp;

public interface ITotpService
{
    /// <summary>Returns a fresh base32 secret (160 bits / 20 bytes — the RFC 6238 recommended size).</summary>
    string GenerateSecret();

    /// <summary>Builds the standard otpauth URI consumable by Authy / Google Authenticator / 1Password.</summary>
    string BuildOtpAuthUri(string issuer, string accountLabel, string base32Secret);

    /// <summary>Verifies a 6-digit code against the secret with ±1 step (30s) tolerance.</summary>
    bool Verify(string base32Secret, string code);
}

internal sealed class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        var bytes = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(bytes);
    }

    public string BuildOtpAuthUri(string issuer, string accountLabel, string base32Secret)
    {
        var encodedIssuer = HttpUtility.UrlEncode(issuer);
        var encodedLabel = HttpUtility.UrlEncode(accountLabel);
        return $"otpauth://totp/{encodedIssuer}:{encodedLabel}?secret={base32Secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool Verify(string base32Secret, string code)
    {
        if (string.IsNullOrEmpty(base32Secret) || string.IsNullOrEmpty(code)) return false;

        byte[] bytes;
        try { bytes = Base32Encoding.ToBytes(base32Secret); }
        catch { return false; }

        var totp = new OtpNet.Totp(bytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }
}
