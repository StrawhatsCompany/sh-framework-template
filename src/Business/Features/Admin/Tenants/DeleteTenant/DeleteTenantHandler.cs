using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.DeleteTenant;

public sealed class DeleteTenantHandler(ITenantStore tenants, IUserContext userCtx)
    : RequestHandler<DeleteTenantCommand>
{
    public override async Task<Result> HandleAsync(
        DeleteTenantCommand request, CancellationToken cancellationToken = default)
    {
        var deleted = await tenants.SoftDeleteAsync(request.Id, userCtx.UserId, cancellationToken);
        return deleted
            ? Result.Success()
            : Result.Failure(IdentityResultCode.TenantNotFound);
    }
}
