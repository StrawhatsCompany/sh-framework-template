using Microsoft.AspNetCore.Authorization;

namespace Business.Libraries.Authentication.Authorization;

/// <summary>
/// Sugar over <c>[Authorize(Policy = ...)]</c> for the permission policy provider. Use on
/// controllers / minimal-API delegates that need a specific permission.
/// <code>
/// [HasPermission("orders.write")]
/// public sealed class CreateOrderEndpoint : IEndpoint { ... }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = PermissionPolicyProvider.PolicyName(permission);
    }

    public string Permission { get; }
}
