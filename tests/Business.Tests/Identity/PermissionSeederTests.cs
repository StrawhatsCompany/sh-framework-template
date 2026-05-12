using Business.Identity;

namespace Business.Tests.Identity;

public class PermissionSeederTests
{
    [Fact]
    public async Task Seeds_admin_catalog_on_startup()
    {
        var store = new InMemoryPermissionStore();
        var seeder = new PermissionSeeder(store);

        await seeder.StartAsync(default);

        var all = await store.ListAsync();
        Assert.Contains(all, p => p.Name == "admin.tenants.read");
        Assert.Contains(all, p => p.Name == "admin.users.write");
        Assert.Contains(all, p => p.Name == "admin.permissions.write");
        Assert.Contains(all, p => p.Name == "api-keys.write");
        // Category is the first dotted segment, so we expect "admin" or "api-keys".
        Assert.All(all, p => Assert.Equal(p.Name.Split('.', 2)[0], p.Category));
    }

    [Fact]
    public async Task Is_idempotent_when_run_twice()
    {
        var store = new InMemoryPermissionStore();
        var seeder = new PermissionSeeder(store);

        await seeder.StartAsync(default);
        var firstCount = (await store.ListAsync()).Count;
        await seeder.StartAsync(default);
        var secondCount = (await store.ListAsync()).Count;

        Assert.Equal(firstCount, secondCount);
    }
}
