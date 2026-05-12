namespace Business.Features.Admin.Tenants.UpdateTenant;

public sealed class UpdateTenantResponse
{
    public required TenantDto Tenant { get; init; }
}
