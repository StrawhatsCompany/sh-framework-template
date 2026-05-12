namespace Business.Authentication.Mfa;

public sealed class MfaOptions
{
    public const string SectionName = "Authentication:Mfa";

    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The TOTP issuer label baked into the otpauth URI. Authenticator apps show this above
    /// the OTP code so users can tell which account it belongs to. Typically the product name.
    /// </summary>
    public string TotpIssuer { get; set; } = "SHFramework";
}
