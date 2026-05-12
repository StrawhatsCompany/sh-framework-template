using System.Security.Claims;
using Business.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Business.Authentication.Authorization;

/// <summary>
/// Resolves a <see cref="PermissionRequirement"/> by walking
/// user (sub) → user-roles → role-permissions → matching the required name.
/// Reads the DB-persisted catalog every request so role changes take effect immediately;
/// rely on a request-scoped cache (or store impls that cache) for performance later.
/// </summary>
internal sealed class PermissionAuthorizationHandler(
    IUserStore users,
    IRoleStore roles,
    IPermissionStore permissions)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var subClaim = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                       ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tidClaim = context.User.FindFirst("tid")?.Value;

        if (!Guid.TryParse(subClaim, out var userId) || !Guid.TryParse(tidClaim, out var tenantId))
        {
            return;
        }

        var user = await users.FindByIdAsync(tenantId, userId);
        if (user is null) return;

        var userRoles = await users.ListRolesAsync(tenantId, userId);
        if (userRoles.Count == 0) return;

        // Resolve permissions across all roles. Stop as soon as we match.
        foreach (var role in userRoles)
        {
            var rolePerms = await roles.ListPermissionsAsync(tenantId, role.Id);
            if (rolePerms.Any(p => string.Equals(p.Name, requirement.Permission, StringComparison.OrdinalIgnoreCase)))
            {
                // Verify the catalog still has it — protects against stale role-permission joins
                // referencing a since-deleted permission.
                if (await permissions.FindByNameAsync(requirement.Permission) is not null)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}
