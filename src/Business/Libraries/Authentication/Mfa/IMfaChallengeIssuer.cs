namespace Business.Libraries.Authentication.Mfa;

public interface IMfaChallengeIssuer
{
    /// <summary>
    /// Generates a code, hashes it, calls the matching <see cref="IMfaChannel"/>'s SendAsync to
    /// deliver the plaintext, persists the challenge, and returns it. The <see cref="MfaChallenge.CodeHash"/>
    /// is the only persisted code — the plaintext only exists between generation and the channel's
    /// SendAsync.
    /// </summary>
    Task<MfaChallenge> IssueAsync(string userId, string channelType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up the challenge, checks expiry + attempt budget, delegates to the channel's
    /// VerifyAsync (so TOTP can do its own math), and on success removes the challenge from the
    /// store.
    /// </summary>
    Task<MfaResult> VerifyAsync(string challengeId, string submittedCode, CancellationToken cancellationToken = default);
}
