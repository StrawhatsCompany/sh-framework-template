using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Users.SetUserRoles;

public sealed class SetUserRolesCommand : Request
{
    public Guid UserId { get; set; }
    public IReadOnlyCollection<Guid> RoleIds { get; set; } = [];
}

public sealed class SetUserRolesHandler(IUserStore users, IRoleStore roles, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<SetUserRolesCommand>
{
    public override async Task<Result> HandleAsync(
        SetUserRolesCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure(IdentityResultCode.TenantRequired);
        }

        var user = await users.FindByIdAsync(tenantId, request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityResultCode.UserNotFound);
        }

        foreach (var roleId in request.RoleIds)
        {
            if (await roles.FindByIdAsync(tenantId, roleId, cancellationToken) is null)
            {
                return Result.Failure(IdentityResultCode.RoleNotFound);
            }
        }

        await users.SetRolesAsync(tenantId, request.UserId, request.RoleIds, userCtx.UserId, cancellationToken);
        return Result.Success();
    }
}
