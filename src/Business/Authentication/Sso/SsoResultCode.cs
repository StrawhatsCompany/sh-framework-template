using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication.Sso;

public static class SsoResultCode
{
    private const string Category = "Sso";

    // 4400-4499 — SSO failures
    public static ResultCode ProviderNotFound => ResultCode.Instance(4400, Category, "SSO provider not found");
    public static ResultCode ProviderNameAlreadyExists => ResultCode.Instance(4401, Category, "An SSO provider with this name already exists in the tenant");
    public static ResultCode ProviderDisabled => ResultCode.Instance(4402, Category, "SSO provider is disabled");
    public static ResultCode InvalidStateCookie => ResultCode.Instance(4403, Category, "OAuth state could not be validated");
    public static ResultCode CodeExchangeFailed => ResultCode.Instance(4404, Category, "Failed to exchange authorization code at the token endpoint");
    public static ResultCode IdTokenInvalid => ResultCode.Instance(4405, Category, "id_token validation failed");
    public static ResultCode UserProvisioningRefused => ResultCode.Instance(4406, Category, "No matching user and auto-provisioning is disabled");
    public static ResultCode ReturnUrlNotAllowed => ResultCode.Instance(4407, Category, "Return URL is not in the allowed list");
    public static ResultCode JwksFetchFailed => ResultCode.Instance(4408, Category, "Failed to fetch JWKS from the provider");
}
