using System.Security.Claims;
using Business.Libraries.Authentication.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Business.Tests.Libraries.Authentication.Authorization;

public class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Succeeds_when_resolver_returns_required_permission()
    {
        var resolver = new StubResolver(["orders.read"]);
        var handler = new PermissionAuthorizationHandler(resolver);
        var requirement = new PermissionRequirement("orders.read");
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity("test")), null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Does_not_succeed_when_permission_missing()
    {
        var resolver = new StubResolver(["orders.read"]);
        var handler = new PermissionAuthorizationHandler(resolver);
        var requirement = new PermissionRequirement("orders.write");
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity("test")), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private sealed class StubResolver(IEnumerable<string> permissions) : IPermissionResolver
    {
        private readonly HashSet<string> _perms = new(permissions, StringComparer.Ordinal);
        public IReadOnlySet<string> Resolve(ClaimsPrincipal principal) => _perms;
    }
}
