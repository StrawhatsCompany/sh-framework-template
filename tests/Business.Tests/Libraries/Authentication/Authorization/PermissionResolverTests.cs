using System.Security.Claims;
using Business.Libraries.Authentication.Authorization;
using Microsoft.Extensions.Options;

namespace Business.Tests.Libraries.Authentication.Authorization;

public class PermissionResolverTests
{
    [Fact]
    public void Resolves_permissions_from_role_map()
    {
        var resolver = NewResolver(("Admin", new[] { "orders.read", "orders.write" }));
        var principal = Principal(roles: ["Admin"]);

        var perms = resolver.Resolve(principal);

        Assert.Contains("orders.read", perms);
        Assert.Contains("orders.write", perms);
    }

    [Fact]
    public void Combines_permissions_from_multiple_roles()
    {
        var resolver = NewResolver(
            ("Admin", new[] { "orders.read" }),
            ("Operator", new[] { "orders.write" }));
        var principal = Principal(roles: ["Admin", "Operator"]);

        var perms = resolver.Resolve(principal);

        Assert.Contains("orders.read", perms);
        Assert.Contains("orders.write", perms);
    }

    [Fact]
    public void Includes_direct_permission_claims_independent_of_roles()
    {
        var resolver = NewResolver();
        var principal = Principal(permissions: ["beta.preview"]);

        var perms = resolver.Resolve(principal);

        Assert.Contains("beta.preview", perms);
    }

    [Fact]
    public void Returns_empty_for_unauthenticated_principal()
    {
        var resolver = NewResolver(("Admin", new[] { "orders.read" }));
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var perms = resolver.Resolve(principal);

        Assert.Empty(perms);
    }

    [Fact]
    public void Returns_empty_for_role_without_mapping()
    {
        var resolver = NewResolver();
        var principal = Principal(roles: ["Unknown"]);

        var perms = resolver.Resolve(principal);

        Assert.Empty(perms);
    }

    private static PermissionResolver NewResolver(params (string Role, string[] Permissions)[] roleMap)
    {
        var options = Options.Create(new SHAuthorizationOptions
        {
            Roles = roleMap.ToDictionary(r => r.Role, r => r.Permissions, StringComparer.Ordinal),
        });
        return new PermissionResolver(options);
    }

    private static (string Role, string[] Permissions) Map(string role, params string[] permissions) => (role, permissions);

    private static ClaimsPrincipal Principal(string[]? roles = null, string[]? permissions = null)
    {
        var claims = new List<Claim>();
        foreach (var r in roles ?? []) claims.Add(new Claim(ClaimTypes.Role, r));
        foreach (var p in permissions ?? []) claims.Add(new Claim(AuthorizationClaims.Permission, p));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
