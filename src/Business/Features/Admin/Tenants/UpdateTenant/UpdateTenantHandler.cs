using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.UpdateTenant;

public sealed class UpdateTenantHandler(ITenantStore tenants, IUserContext userCtx)
    : RequestHandler<UpdateTenantCommand, UpdateTenantResponse>
{
    public override async Task<Result<UpdateTenantResponse>> HandleAsync(
        UpdateTenantCommand request, CancellationToken cancellationToken = default)
    {
        var tenant = await tenants.FindByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<UpdateTenantResponse>(IdentityResultCode.TenantNotFound);
        }

        if (request.DisplayName is { } displayName && !string.IsNullOrWhiteSpace(displayName))
        {
            tenant.DisplayName = displayName.Trim();
        }

        if (request.Status is { } status)
        {
            tenant.Status = status;
        }

        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedBy = userCtx.UserId;

        await tenants.UpdateAsync(tenant, cancellationToken);

        return Result.Success(new UpdateTenantResponse { Tenant = TenantDto.From(tenant) });
    }
}
