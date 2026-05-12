using Business.Identity;
using Domain.Entities.Identity;

namespace Business.Tests.Identity;

public class InMemoryTenantStoreTests
{
    [Fact]
    public async Task ListAsync_excludes_soft_deleted_rows()
    {
        var store = new InMemoryTenantStore();
        var alive = Make("acme");
        var dead = Make("zombie", deleted: true);
        await store.AddAsync(alive);
        await store.AddAsync(dead);

        var all = await store.ListAsync();

        Assert.Single(all);
        Assert.Equal("acme", all[0].Slug);
    }

    [Fact]
    public async Task FindByIdAsync_returns_null_for_soft_deleted()
    {
        var store = new InMemoryTenantStore();
        var tenant = Make("acme", deleted: true);
        await store.AddAsync(tenant);

        Assert.Null(await store.FindByIdAsync(tenant.Id));
    }

    [Fact]
    public async Task FindBySlugAsync_is_case_insensitive()
    {
        var store = new InMemoryTenantStore();
        await store.AddAsync(Make("acme"));

        Assert.NotNull(await store.FindBySlugAsync("ACME"));
        Assert.NotNull(await store.FindBySlugAsync("Acme"));
    }

    [Fact]
    public async Task SoftDeleteAsync_sets_DeletedAt_and_DeletedBy()
    {
        var store = new InMemoryTenantStore();
        var tenant = Make("acme");
        await store.AddAsync(tenant);
        var deleter = Guid.NewGuid();

        var ok = await store.SoftDeleteAsync(tenant.Id, deleter);

        Assert.True(ok);
        Assert.NotNull(tenant.DeletedAt);
        Assert.Equal(deleter, tenant.DeletedBy);
    }

    [Fact]
    public async Task SoftDeleteAsync_returns_false_for_unknown_id()
    {
        var store = new InMemoryTenantStore();

        Assert.False(await store.SoftDeleteAsync(Guid.NewGuid(), null));
    }

    [Fact]
    public async Task UpdateAsync_refuses_soft_deleted_rows()
    {
        var store = new InMemoryTenantStore();
        var tenant = Make("acme", deleted: true);
        await store.AddAsync(tenant);

        Assert.Null(await store.UpdateAsync(tenant));
    }

    private static Tenant Make(string slug, bool deleted = false) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        DisplayName = slug,
        Status = TenantStatus.Active,
        CreatedAt = DateTime.UtcNow,
        DeletedAt = deleted ? DateTime.UtcNow : null,
    };
}
