namespace Business.Features.Admin.Tenants.CreateTenant;

public sealed class CreateTenantResponse
{
    public required TenantDto Tenant { get; init; }
}
