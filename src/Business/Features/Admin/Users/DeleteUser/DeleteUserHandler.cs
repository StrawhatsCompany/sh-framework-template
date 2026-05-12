using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Users.DeleteUser;

public sealed class DeleteUserCommand : Request
{
    public Guid Id { get; set; }
}

public sealed class DeleteUserHandler(IUserStore users, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<DeleteUserCommand>
{
    public override async Task<Result> HandleAsync(
        DeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure(IdentityResultCode.TenantRequired);
        }
        var ok = await users.SoftDeleteAsync(tenantId, request.Id, userCtx.UserId, cancellationToken);
        return ok ? Result.Success() : Result.Failure(IdentityResultCode.UserNotFound);
    }
}
