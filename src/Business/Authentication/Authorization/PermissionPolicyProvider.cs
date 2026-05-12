using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Business.Authentication.Authorization;

/// <summary>
/// Dynamic policy provider that builds a policy per permission name on demand. Endpoints
/// declare permissions via <see cref="HasPermissionAttribute"/>, which encodes the policy
/// name as <c>perm:&lt;permission&gt;</c>. This provider strips the prefix and creates a
/// policy requiring a single <see cref="PermissionRequirement"/>.
/// </summary>
internal sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}
