using System.Security.Claims;

namespace Business.Libraries.Authentication.Authorization;

/// <summary>
/// Given a principal (with role claims), return the set of permissions they hold. The default
/// implementation reads a role→permissions map from <see cref="SHAuthorizationOptions"/>.
/// Consumers override with a DB / OPA / external-service-backed implementation when role-to-
/// permission mapping is dynamic.
/// </summary>
public interface IPermissionResolver
{
    IReadOnlySet<string> Resolve(ClaimsPrincipal principal);
}
