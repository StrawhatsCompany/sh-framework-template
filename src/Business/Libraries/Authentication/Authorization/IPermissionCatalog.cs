namespace Business.Libraries.Authentication.Authorization;

/// <summary>
/// Set of permission strings the application knows about. Anything an endpoint guards with
/// <c>[HasPermission]</c> / <c>RequirePermission</c> must be in the catalog, so typos and stale
/// references fail loudly at startup rather than silently bypassing authorization.
/// </summary>
public interface IPermissionCatalog
{
    IReadOnlySet<string> KnownPermissions { get; }
    bool Contains(string permission);
}
