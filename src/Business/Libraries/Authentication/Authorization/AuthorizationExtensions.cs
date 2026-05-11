using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Business.Libraries.Authentication.Authorization;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Wires the permission-based authorization model: policy provider (lazily builds a policy per
    /// permission), authorization handler, permission resolver (role → permissions via
    /// <see cref="SHAuthorizationOptions"/>), and a permission catalog populated by
    /// <paramref name="configurePermissions"/>. Any permission name referenced from a
    /// <c>[HasPermission]</c> attribute or <c>.RequirePermission(...)</c> must be registered here.
    /// </summary>
    public static SHAuthenticationBuilder AddAuthorizationModel(
        this SHAuthenticationBuilder builder,
        Action<PermissionRegistrar>? configurePermissions = null)
    {
        builder.Services.Configure<SHAuthorizationOptions>(builder.Configuration.GetSection(SHAuthorizationOptions.SectionName));

        var registrar = new PermissionRegistrar();
        configurePermissions?.Invoke(registrar);

        builder.Services.AddSingleton<IPermissionCatalog>(_ => new PermissionCatalog(registrar.Permissions));
        builder.Services.TryAddSingleton<IPermissionResolver, PermissionResolver>();
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        return builder;
    }
}

public sealed class PermissionRegistrar
{
    private readonly HashSet<string> _permissions = new(StringComparer.Ordinal);

    public IEnumerable<string> Permissions => _permissions;

    public PermissionRegistrar Add(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission must be a non-empty string.", nameof(permission));
        }
        _permissions.Add(permission);
        return this;
    }

    public PermissionRegistrar Add(params string[] permissions)
    {
        foreach (var p in permissions)
        {
            Add(p);
        }
        return this;
    }
}
