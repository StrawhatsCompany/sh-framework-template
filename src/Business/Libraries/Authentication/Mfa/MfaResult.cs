namespace Business.Libraries.Authentication.Mfa;

public enum MfaResult
{
    Success,
    Invalid,
    Expired,
    NoSuchChallenge,
    TooManyAttempts,
}

public sealed record MfaSendResult(bool IsSent, string? FailureReason = null)
{
    public static MfaSendResult Sent { get; } = new(true);
    public static MfaSendResult Failed(string reason) => new(false, reason);
}

public sealed record MfaVerifyResult(bool IsValid, string? FailureReason = null)
{
    public static MfaVerifyResult Valid { get; } = new(true);
    public static MfaVerifyResult Invalid(string reason) => new(false, reason);
}
