using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Identity;

public static class IdentityResultCode
{
    private const string Category = "Identity";

    public static ResultCode TenantNotFound => ResultCode.Instance(3000, Category, "Tenant not found");
    public static ResultCode TenantSlugAlreadyExists => ResultCode.Instance(3001, Category, "A tenant with this slug already exists");
    public static ResultCode TenantSlugInvalid => ResultCode.Instance(3002, Category, "Tenant slug must be 2-64 lowercase alphanumeric characters or hyphens");
}
