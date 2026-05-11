using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Business.Libraries.Authentication.Authorization;

/// <summary>
/// Generates an <see cref="AuthorizationPolicy"/> on demand for any permission string so consumers
/// don't have to call <c>AddPolicy</c> per permission. Falls through to the default provider for
/// non-permission policy names. Throws at policy build time if the requested permission isn't in
/// the catalog — typos fail loudly.
/// </summary>
internal sealed class PermissionPolicyProvider(
    IOptions<AuthorizationOptions> options,
    IPermissionCatalog catalog) : DefaultAuthorizationPolicyProvider(options)
{
    private const string PermissionPolicyPrefix = "permission:";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[PermissionPolicyPrefix.Length..];
            if (!catalog.Contains(permission))
            {
                throw new InvalidOperationException(
                    $"Permission '{permission}' is not in the catalog. Register it via AddSHAuthentication(..., auth => auth.AddAuthorizationModel(perms => perms.Add(\"{permission}\")) ...).");
            }
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }

    /// <summary>Builds the policy name a consumer attaches via <c>[Authorize(Policy = ...)]</c>.</summary>
    public static string PolicyName(string permission) => PermissionPolicyPrefix + permission;
}
