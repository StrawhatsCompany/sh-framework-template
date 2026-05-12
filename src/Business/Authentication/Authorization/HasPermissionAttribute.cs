using Microsoft.AspNetCore.Authorization;

namespace Business.Authentication.Authorization;

/// <summary>
/// Gates an endpoint behind a permission. The policy resolves against the DB-persisted
/// catalog at request time — user → roles → permissions — so role changes take effect
/// immediately without waiting for JWT expiry.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public HasPermissionAttribute(string permission) : base($"{PolicyPrefix}{permission}")
    {
        Permission = permission;
    }

    public string Permission { get; }
}
