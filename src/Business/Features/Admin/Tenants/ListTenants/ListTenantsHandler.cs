using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.ListTenants;

public sealed class ListTenantsHandler(ITenantStore tenants)
    : RequestHandler<ListTenantsQuery, ListTenantsResponse>
{
    public override async Task<Result<ListTenantsResponse>> HandleAsync(
        ListTenantsQuery request, CancellationToken cancellationToken = default)
    {
        var all = await tenants.ListAsync(cancellationToken);
        var filtered = request.Status is { } status
            ? all.Where(t => t.Status == status)
            : all;

        return Result.Success(new ListTenantsResponse
        {
            Items = filtered.Select(TenantDto.From).ToList(),
        });
    }
}
