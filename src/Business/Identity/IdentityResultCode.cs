using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Identity;

public static class IdentityResultCode
{
    private const string Category = "Identity";

    // Tenant (3000-3099)
    public static ResultCode TenantNotFound => ResultCode.Instance(3000, Category, "Tenant not found");
    public static ResultCode TenantSlugAlreadyExists => ResultCode.Instance(3001, Category, "A tenant with this slug already exists");
    public static ResultCode TenantSlugInvalid => ResultCode.Instance(3002, Category, "Tenant slug must be 2-64 lowercase alphanumeric characters or hyphens");
    public static ResultCode TenantRequired => ResultCode.Instance(3003, Category, "Tenant context is required for this operation");

    // User (3100-3199)
    public static ResultCode UserNotFound => ResultCode.Instance(3100, Category, "User not found");
    public static ResultCode UserEmailAlreadyExists => ResultCode.Instance(3101, Category, "A user with this email already exists in the tenant");
    public static ResultCode UserUsernameAlreadyExists => ResultCode.Instance(3102, Category, "A user with this username already exists in the tenant");
    public static ResultCode UserEmailInvalid => ResultCode.Instance(3103, Category, "Email is not a valid address");
    public static ResultCode UserUsernameInvalid => ResultCode.Instance(3104, Category, "Username must be 2-64 alphanumeric characters, dots, dashes, or underscores");
    public static ResultCode UserPasswordTooWeak => ResultCode.Instance(3105, Category, "Password must be at least 12 characters");

    // Role (3200-3299)
    public static ResultCode RoleNotFound => ResultCode.Instance(3200, Category, "Role not found");
    public static ResultCode RoleNameAlreadyExists => ResultCode.Instance(3201, Category, "A role with this name already exists in the tenant");
    public static ResultCode RoleNameInvalid => ResultCode.Instance(3202, Category, "Role name must be 2-64 characters");
    public static ResultCode RoleSystemImmutable => ResultCode.Instance(3203, Category, "System roles cannot be modified or deleted");

    // Permission (3300-3399)
    public static ResultCode PermissionNotFound => ResultCode.Instance(3300, Category, "Permission not found");
    public static ResultCode PermissionNameAlreadyExists => ResultCode.Instance(3301, Category, "A permission with this name already exists");
    public static ResultCode PermissionNameInvalid => ResultCode.Instance(3302, Category, "Permission name must be dotted lowercase (e.g. admin.users.write)");
}
