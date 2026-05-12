using Business.Features.Admin.Tenants.GetTenant;
using Business.Identity;
using Domain.Entities.Identity;

namespace Business.Tests.Features.Admin.Tenants;

public class GetTenantHandlerTests
{
    [Fact]
    public async Task Returns_tenant_when_present()
    {
        var store = new InMemoryTenantStore();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = "acme",
            DisplayName = "Acme",
            CreatedAt = DateTime.UtcNow,
        };
        await store.AddAsync(tenant);
        var handler = new GetTenantHandler(store);

        var result = await handler.HandleAsync(new GetTenantQuery { Id = tenant.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal(tenant.Id, result.Data!.Tenant.Id);
    }

    [Fact]
    public async Task Returns_TenantNotFound_for_missing_id()
    {
        var handler = new GetTenantHandler(new InMemoryTenantStore());

        var result = await handler.HandleAsync(new GetTenantQuery { Id = Guid.NewGuid() });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.TenantNotFound.Code, result.Code);
    }
}
