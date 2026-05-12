using Microsoft.AspNetCore.Builder;

namespace Business.Authentication.Authorization;

public static class EndpointConventionBuilderExtensions
{
    /// <summary>
    /// Gates a minimal-API endpoint behind a permission. Resolves via the dynamic
    /// <see cref="PermissionPolicyProvider"/> at request time.
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization($"{HasPermissionAttribute.PolicyPrefix}{permission}");
    }
}
