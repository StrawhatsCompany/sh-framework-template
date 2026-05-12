using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Roles.ListRoles;

public sealed class ListRolesQuery : Request<ListRolesResponse> { }

public sealed class ListRolesResponse
{
    public required IReadOnlyList<RoleDto> Items { get; init; }
}

public sealed class ListRolesHandler(IRoleStore roles, ITenantContext tenantCtx)
    : RequestHandler<ListRolesQuery, ListRolesResponse>
{
    public override async Task<Result<ListRolesResponse>> HandleAsync(
        ListRolesQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<ListRolesResponse>(IdentityResultCode.TenantRequired);
        }

        var all = await roles.ListAsync(tenantId, cancellationToken);
        return Result.Success(new ListRolesResponse
        {
            Items = all.Select(RoleDto.From).ToList(),
        });
    }
}
