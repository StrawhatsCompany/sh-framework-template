using Microsoft.AspNetCore.Builder;

namespace Business.Libraries.Authentication.Authorization;

public static class EndpointConventionBuilderExtensions
{
    /// <summary>
    /// Minimal-API equivalent of <see cref="HasPermissionAttribute"/>: requires the principal to
    /// hold <paramref name="permission"/>.
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(PermissionPolicyProvider.PolicyName(permission));
}
