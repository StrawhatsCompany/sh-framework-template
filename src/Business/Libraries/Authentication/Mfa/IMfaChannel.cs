namespace Business.Libraries.Authentication.Mfa;

/// <summary>
/// Consumer-implemented contract for one MFA channel. Implementations:
/// <list type="bullet">
///   <item><description><b>SMS</b> wraps an SMS provider (scaffolded by <c>shf make:provider Sms</c>) and pushes the
///   plaintext code in <c>SendAsync</c>. <c>VerifyAsync</c> is a no-op that returns <see cref="MfaVerifyResult.Valid"/>
///   (the orchestrator does the hash comparison).</description></item>
///   <item><description><b>Email</b> wraps the Mail provider analogously.</description></item>
///   <item><description><b>TOTP</b>: <c>SendAsync</c> is a no-op (user reads the code from their authenticator app);
///   <c>VerifyAsync</c> does the RFC 6238 math directly against the user's stored secret.</description></item>
/// </list>
/// </summary>
public interface IMfaChannel
{
    /// <summary>Channel discriminator (e.g. <c>"Sms"</c>, <c>"Email"</c>, <c>"Totp"</c>).</summary>
    string ChannelType { get; }

    /// <summary>
    /// Pushes the plaintext <paramref name="plaintextCode"/> to the user via this channel. The
    /// orchestrator generates and hashes the code; <paramref name="plaintextCode"/> is passed
    /// here only because the channel needs to deliver it. For TOTP this is a no-op.
    /// </summary>
    Task<MfaSendResult> SendAsync(MfaChallenge challenge, string plaintextCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Channel-specific verify hook. For Sms / Email this is typically a no-op returning
    /// <see cref="MfaVerifyResult.Valid"/>; the orchestrator handles hash comparison. For TOTP,
    /// the channel does the RFC 6238 math against the user's stored secret and returns the result.
    /// </summary>
    Task<MfaVerifyResult> VerifyAsync(MfaChallenge challenge, string submittedCode, CancellationToken cancellationToken = default);
}
