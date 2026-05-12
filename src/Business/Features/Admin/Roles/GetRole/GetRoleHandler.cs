using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Roles.GetRole;

public sealed class GetRoleQuery : Request<GetRoleResponse>
{
    public Guid Id { get; set; }
}

public sealed class GetRoleResponse
{
    public required RoleDto Role { get; init; }
}

public sealed class GetRoleHandler(IRoleStore roles, ITenantContext tenantCtx)
    : RequestHandler<GetRoleQuery, GetRoleResponse>
{
    public override async Task<Result<GetRoleResponse>> HandleAsync(
        GetRoleQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<GetRoleResponse>(IdentityResultCode.TenantRequired);
        }

        var role = await roles.FindByIdAsync(tenantId, request.Id, cancellationToken);
        return role is null
            ? Result.Failure<GetRoleResponse>(IdentityResultCode.RoleNotFound)
            : Result.Success(new GetRoleResponse { Role = RoleDto.From(role) });
    }
}
