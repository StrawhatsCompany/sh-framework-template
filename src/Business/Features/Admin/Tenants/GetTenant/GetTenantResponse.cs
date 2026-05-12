namespace Business.Features.Admin.Tenants.GetTenant;

public sealed class GetTenantResponse
{
    public required TenantDto Tenant { get; init; }
}
