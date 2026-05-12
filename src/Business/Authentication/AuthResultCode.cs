using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication;

public static class AuthResultCode
{
    private const string Category = "Auth";

    // 4000-4099 — login failures (deliberately generic where it'd leak info)
    public static ResultCode InvalidCredentials => ResultCode.Instance(4000, Category, "Invalid credentials");
    public static ResultCode UserDisabled => ResultCode.Instance(4001, Category, "User account is disabled");
    public static ResultCode UserLocked => ResultCode.Instance(4002, Category, "User account is locked");
    public static ResultCode UserPendingVerification => ResultCode.Instance(4003, Category, "Email verification required before sign-in");
    public static ResultCode TenantNotFound => ResultCode.Instance(4004, Category, "Tenant could not be resolved");
    public static ResultCode TenantSuspended => ResultCode.Instance(4005, Category, "Tenant is suspended");

    // 4100-4199 — session / refresh-token failures
    public static ResultCode RefreshTokenNotFound => ResultCode.Instance(4100, Category, "Refresh token is not recognised");
    public static ResultCode RefreshTokenExpired => ResultCode.Instance(4101, Category, "Refresh token has expired");
    public static ResultCode RefreshTokenRevoked => ResultCode.Instance(4102, Category, "Refresh token has been revoked");
    public static ResultCode RefreshTokenReused => ResultCode.Instance(4103, Category, "Refresh token reuse detected; session family revoked");
    public static ResultCode SessionRevoked => ResultCode.Instance(4104, Category, "Session has been revoked");
    public static ResultCode SessionExpired => ResultCode.Instance(4105, Category, "Session has expired");
    public static ResultCode SessionNotFound => ResultCode.Instance(4106, Category, "Session not found");
}
