using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication.Mfa;

public static class MfaResultCode
{
    private const string Category = "Mfa";

    // 4300-4399 — MFA failures
    public static ResultCode FactorNotFound => ResultCode.Instance(4300, Category, "MFA factor not found");
    public static ResultCode FactorNotActive => ResultCode.Instance(4301, Category, "MFA factor is not active");
    public static ResultCode FactorAlreadyEnrolled => ResultCode.Instance(4302, Category, "An MFA factor of this kind is already enrolled");
    public static ResultCode ChallengeNotFound => ResultCode.Instance(4303, Category, "MFA challenge not found");
    public static ResultCode ChallengeExpired => ResultCode.Instance(4304, Category, "MFA challenge has expired");
    public static ResultCode ChallengeAlreadyConsumed => ResultCode.Instance(4305, Category, "MFA challenge has already been used");
    public static ResultCode ChallengeFailed => ResultCode.Instance(4306, Category, "MFA challenge failed too many times; request a new one");
    public static ResultCode InvalidCode => ResultCode.Instance(4307, Category, "Invalid MFA code");
    public static ResultCode KindUnsupported => ResultCode.Instance(4308, Category, "No channel registered for this factor kind");
    public static ResultCode RateLimited => ResultCode.Instance(4309, Category, "Too many MFA dispatch requests; try again later");
    public static ResultCode DispatchFailed => ResultCode.Instance(4310, Category, "Failed to dispatch MFA code via the channel provider");
}
