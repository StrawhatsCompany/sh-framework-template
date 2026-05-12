using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Roles.SetRolePermissions;

public sealed class SetRolePermissionsCommand : Request
{
    public Guid RoleId { get; set; }
    public IReadOnlyCollection<Guid> PermissionIds { get; set; } = [];
}

public sealed class SetRolePermissionsHandler(
    IRoleStore roles, IPermissionStore permissions, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<SetRolePermissionsCommand>
{
    public override async Task<Result> HandleAsync(
        SetRolePermissionsCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure(IdentityResultCode.TenantRequired);
        }

        var role = await roles.FindByIdAsync(tenantId, request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(IdentityResultCode.RoleNotFound);
        }

        foreach (var permId in request.PermissionIds)
        {
            if (await permissions.FindByIdAsync(permId, cancellationToken) is null)
            {
                return Result.Failure(IdentityResultCode.PermissionNotFound);
            }
        }

        await roles.SetPermissionsAsync(tenantId, request.RoleId, request.PermissionIds, userCtx.UserId, cancellationToken);
        return Result.Success();
    }
}
