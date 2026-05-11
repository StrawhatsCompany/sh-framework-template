using Business.Libraries.Authentication.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Business.Tests.Libraries.Authentication.Authorization;

public class PermissionPolicyProviderTests
{
    [Fact]
    public async Task Builds_policy_lazily_for_known_permission()
    {
        var catalog = new PermissionCatalog(["orders.read"]);
        var provider = new PermissionPolicyProvider(NewOptions(), catalog);

        var policy = await provider.GetPolicyAsync(PermissionPolicyProvider.PolicyName("orders.read"));

        Assert.NotNull(policy);
        Assert.Contains(policy!.Requirements, r => r is PermissionRequirement pr && pr.Permission == "orders.read");
    }

    [Fact]
    public async Task Throws_on_unknown_permission()
    {
        var catalog = new PermissionCatalog(["orders.read"]);
        var provider = new PermissionPolicyProvider(NewOptions(), catalog);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetPolicyAsync(PermissionPolicyProvider.PolicyName("orders.purge")));
    }

    [Fact]
    public async Task Falls_through_to_default_provider_for_non_permission_policies()
    {
        var catalog = new PermissionCatalog([]);
        var options = Options.Create(new AuthorizationOptions());
        options.Value.AddPolicy("CustomPolicy", b => b.RequireAuthenticatedUser());
        var provider = new PermissionPolicyProvider(options, catalog);

        var policy = await provider.GetPolicyAsync("CustomPolicy");

        Assert.NotNull(policy);
    }

    private static IOptions<AuthorizationOptions> NewOptions() => Options.Create(new AuthorizationOptions());
}
