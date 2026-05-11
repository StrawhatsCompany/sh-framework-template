namespace Business.Libraries.Authentication.Authorization;

internal sealed class PermissionCatalog : IPermissionCatalog
{
    public PermissionCatalog(IEnumerable<string> permissions)
    {
        KnownPermissions = new HashSet<string>(permissions, StringComparer.Ordinal);
    }

    public IReadOnlySet<string> KnownPermissions { get; }

    public bool Contains(string permission) => KnownPermissions.Contains(permission);
}
