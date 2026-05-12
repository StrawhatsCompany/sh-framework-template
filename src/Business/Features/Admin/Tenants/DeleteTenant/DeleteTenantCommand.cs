using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.DeleteTenant;

public sealed class DeleteTenantCommand : Request
{
    public Guid Id { get; set; }
}
