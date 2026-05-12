namespace Business.Features.Admin.Tenants.ListTenants;

public sealed class ListTenantsResponse
{
    public required IReadOnlyList<TenantDto> Items { get; init; }
}
