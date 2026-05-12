using Microsoft.AspNetCore.Authorization;

namespace Business.Authentication.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
