using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.GetTenant;

public sealed class GetTenantQuery : Request<GetTenantResponse>
{
    public Guid Id { get; set; }
}
