using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Roles.DeleteRole;

public sealed class DeleteRoleCommand : Request
{
    public Guid Id { get; set; }
}

public sealed class DeleteRoleHandler(IRoleStore roles, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<DeleteRoleCommand>
{
    public override async Task<Result> HandleAsync(
        DeleteRoleCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure(IdentityResultCode.TenantRequired);
        }

        var role = await roles.FindByIdAsync(tenantId, request.Id, cancellationToken);
        if (role is null) return Result.Failure(IdentityResultCode.RoleNotFound);
        if (role.IsSystem) return Result.Failure(IdentityResultCode.RoleSystemImmutable);

        await roles.SoftDeleteAsync(tenantId, request.Id, userCtx.UserId, cancellationToken);
        return Result.Success();
    }
}
