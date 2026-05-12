using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Permissions.GetPermission;

public sealed class GetPermissionQuery : Request<GetPermissionResponse>
{
    public Guid Id { get; set; }
}

public sealed class GetPermissionResponse
{
    public required PermissionDto Permission { get; init; }
}

public sealed class GetPermissionHandler(IPermissionStore permissions)
    : RequestHandler<GetPermissionQuery, GetPermissionResponse>
{
    public override async Task<Result<GetPermissionResponse>> HandleAsync(
        GetPermissionQuery request, CancellationToken cancellationToken = default)
    {
        var permission = await permissions.FindByIdAsync(request.Id, cancellationToken);
        return permission is null
            ? Result.Failure<GetPermissionResponse>(IdentityResultCode.PermissionNotFound)
            : Result.Success(new GetPermissionResponse { Permission = PermissionDto.From(permission) });
    }
}
