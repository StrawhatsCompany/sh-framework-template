using Business.Features.Admin.Tenants.ListTenants;
using Business.Identity;
using Domain.Entities.Identity;

namespace Business.Tests.Features.Admin.Tenants;

public class ListTenantsHandlerTests
{
    [Fact]
    public async Task Returns_all_active_when_no_status_filter()
    {
        var store = await Seed();
        var handler = new ListTenantsHandler(store);

        var result = await handler.HandleAsync(new ListTenantsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Items.Count);
    }

    [Fact]
    public async Task Filters_by_status()
    {
        var store = await Seed();
        var handler = new ListTenantsHandler(store);

        var result = await handler.HandleAsync(new ListTenantsQuery { Status = TenantStatus.Suspended });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
        Assert.Equal("dormant", result.Data.Items[0].Slug);
    }

    [Fact]
    public async Task Excludes_soft_deleted()
    {
        var store = new InMemoryTenantStore();
        await store.AddAsync(new Tenant { Id = Guid.NewGuid(), Slug = "live", Status = TenantStatus.Active });
        await store.AddAsync(new Tenant { Id = Guid.NewGuid(), Slug = "dead", DeletedAt = DateTime.UtcNow });
        var handler = new ListTenantsHandler(store);

        var result = await handler.HandleAsync(new ListTenantsQuery());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
    }

    private static async Task<InMemoryTenantStore> Seed()
    {
        var store = new InMemoryTenantStore();
        await store.AddAsync(new Tenant { Id = Guid.NewGuid(), Slug = "acme", Status = TenantStatus.Active });
        await store.AddAsync(new Tenant { Id = Guid.NewGuid(), Slug = "dormant", Status = TenantStatus.Suspended });
        return store;
    }
}
