using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Business.Libraries.Authentication.Authorization;

internal sealed class PermissionResolver(IOptions<AuthorizationModelOptions> options) : IPermissionResolver
{
    public IReadOnlySet<string> Resolve(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return EmptySet;
        }

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var direct = principal.FindAll(AuthorizationClaims.Permission).Select(c => c.Value);

        var result = new HashSet<string>(direct, StringComparer.Ordinal);
        foreach (var role in roles)
        {
            if (options.Value.Roles.TryGetValue(role, out var perms))
            {
                foreach (var p in perms)
                {
                    result.Add(p);
                }
            }
        }
        return result;
    }

    private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>();
}
