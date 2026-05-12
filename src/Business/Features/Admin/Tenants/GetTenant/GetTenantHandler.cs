using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.GetTenant;

public sealed class GetTenantHandler(ITenantStore tenants)
    : RequestHandler<GetTenantQuery, GetTenantResponse>
{
    public override async Task<Result<GetTenantResponse>> HandleAsync(
        GetTenantQuery request, CancellationToken cancellationToken = default)
    {
        var tenant = await tenants.FindByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<GetTenantResponse>(IdentityResultCode.TenantNotFound);
        }

        return Result.Success(new GetTenantResponse { Tenant = TenantDto.From(tenant) });
    }
}
