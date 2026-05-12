using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.CreateTenant;

public sealed class CreateTenantCommand : Request<CreateTenantResponse>
{
    public string Slug { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
