using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.ListTenants;

public sealed class ListTenantsQuery : Request<ListTenantsResponse>
{
    public TenantStatus? Status { get; set; }
}
