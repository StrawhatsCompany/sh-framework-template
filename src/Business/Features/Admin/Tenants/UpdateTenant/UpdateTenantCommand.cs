using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.UpdateTenant;

public sealed class UpdateTenantCommand : Request<UpdateTenantResponse>
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public TenantStatus? Status { get; set; }
}
