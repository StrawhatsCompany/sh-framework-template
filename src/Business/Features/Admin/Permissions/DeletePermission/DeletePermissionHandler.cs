using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Permissions.DeletePermission;

public sealed class DeletePermissionCommand : Request
{
    public Guid Id { get; set; }
}

public sealed class DeletePermissionHandler(IPermissionStore permissions)
    : RequestHandler<DeletePermissionCommand>
{
    public override async Task<Result> HandleAsync(
        DeletePermissionCommand request, CancellationToken cancellationToken = default)
    {
        var ok = await permissions.DeleteAsync(request.Id, cancellationToken);
        return ok ? Result.Success() : Result.Failure(IdentityResultCode.PermissionNotFound);
    }
}
