namespace Business.Libraries.Authentication.Mfa;

public sealed record MfaChallenge(
    string ChallengeId,
    string UserId,
    string ChannelType,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string CodeHash,
    int Attempts = 0);
