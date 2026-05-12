using System.Security.Claims;
using Business.Authentication.Authorization;
using Business.Identity;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Business.Tests.Authentication.Authorization;

public class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Succeeds_when_user_has_role_with_required_permission()
    {
        var ctx = await BuildContextAsync(grant: "orders.read", require: "orders.read");

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task Fails_when_user_has_no_matching_permission()
    {
        var ctx = await BuildContextAsync(grant: "orders.read", require: "orders.write");

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task Fails_when_required_permission_not_in_catalog()
    {
        var ctx = await BuildContextAsync(grant: "orders.read", require: "orders.read", includePermissionInCatalog: false);

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task Fails_when_principal_has_no_sub_claim()
    {
        var permissions = new InMemoryPermissionStore();
        var roles = new InMemoryRoleStore(permissions);
        var users = new InMemoryUserStore(roles);
        var handler = new PermissionAuthorizationHandler(users, roles, permissions);
        var ctx = new AuthorizationHandlerContext(
            [new PermissionRequirement("orders.read")],
            new ClaimsPrincipal(new ClaimsIdentity()),
            null);

        foreach (var req in ctx.Requirements.OfType<PermissionRequirement>())
        {
            await handler.HandleAsync(ctx);
        }

        Assert.False(ctx.HasSucceeded);
    }

    private static async Task<AuthorizationHandlerContext> BuildContextAsync(
        string grant, string require, bool includePermissionInCatalog = true)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var permissions = new InMemoryPermissionStore();
        var roles = new InMemoryRoleStore(permissions);
        var users = new InMemoryUserStore(roles);

        var perm = new Permission { Id = Guid.NewGuid(), Name = grant, Category = grant.Split('.', 2)[0] };
        if (includePermissionInCatalog)
        {
            await permissions.AddAsync(perm);
        }

        var role = new Role { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Tester" };
        await roles.AddAsync(role);
        await roles.SetPermissionsAsync(tenantId, role.Id, [perm.Id], actingUserId: null, ct: default);

        var user = new User
        {
            Id = userId, TenantId = tenantId, Email = "u@x", Username = "u", DisplayName = "U",
            Status = UserStatus.Active,
        };
        await users.AddAsync(user);
        await users.SetRolesAsync(tenantId, userId, [role.Id], actingUserId: null, ct: default);

        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("tid", tenantId.ToString()),
        ], authenticationType: "test"));

        var ctx = new AuthorizationHandlerContext(
            [new PermissionRequirement(require)],
            principal,
            null);

        var handler = new PermissionAuthorizationHandler(users, roles, permissions);
        await handler.HandleAsync(ctx);
        return ctx;
    }
}
