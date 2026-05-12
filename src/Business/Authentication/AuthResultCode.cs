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
}
