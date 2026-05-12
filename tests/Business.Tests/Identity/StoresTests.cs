using Business.Identity;
using Domain.Entities.Identity;

namespace Business.Tests.Identity;

public class StoresTests
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    [Fact]
    public async Task UserStore_isolates_users_by_tenant()
    {
        var store = NewUserStore();
        await store.AddAsync(new User { Id = Guid.NewGuid(), TenantId = _tenantA, Email = "a@x", Username = "a" });
        await store.AddAsync(new User { Id = Guid.NewGuid(), TenantId = _tenantB, Email = "b@x", Username = "b" });

        Assert.Single(await store.ListAsync(_tenantA));
        Assert.Single(await store.ListAsync(_tenantB));
    }

    [Fact]
    public async Task UserStore_find_by_email_is_case_insensitive_and_tenant_scoped()
    {
        var store = NewUserStore();
        await store.AddAsync(new User { Id = Guid.NewGuid(), TenantId = _tenantA, Email = "user@example.com", Username = "u" });

        Assert.NotNull(await store.FindByEmailAsync(_tenantA, "USER@EXAMPLE.COM"));
        Assert.Null(await store.FindByEmailAsync(_tenantB, "user@example.com"));
    }

    [Fact]
    public async Task UserStore_SetRolesAsync_replaces_assignments()
    {
        var (userStore, roleStore) = NewUserAndRoleStores();
        var userId = Guid.NewGuid();
        await userStore.AddAsync(new User { Id = userId, TenantId = _tenantA, Email = "u@x", Username = "u" });
        var roleA = await AddRole(roleStore, "role-a");
        var roleB = await AddRole(roleStore, "role-b");
        var roleC = await AddRole(roleStore, "role-c");

        await userStore.SetRolesAsync(_tenantA, userId, [roleA.Id, roleB.Id], actingUserId: null, ct: default);
        Assert.Equal(2, (await userStore.ListRolesAsync(_tenantA, userId)).Count);

        await userStore.SetRolesAsync(_tenantA, userId, [roleC.Id], actingUserId: null, ct: default);
        var after = await userStore.ListRolesAsync(_tenantA, userId);
        Assert.Single(after);
        Assert.Equal("role-c", after[0].Name);
    }

    [Fact]
    public async Task RoleStore_refuses_to_soft_delete_system_roles()
    {
        var roleStore = NewRoleStore();
        var system = new Role { Id = Guid.NewGuid(), TenantId = _tenantA, Name = "Administrator", IsSystem = true };
        await roleStore.AddAsync(system);

        var ok = await roleStore.SoftDeleteAsync(_tenantA, system.Id, deletedBy: null);

        Assert.False(ok);
    }

    [Fact]
    public async Task PermissionStore_FindByName_is_case_insensitive()
    {
        var store = new InMemoryPermissionStore();
        await store.AddAsync(new Permission { Id = Guid.NewGuid(), Name = "orders.read", Category = "orders" });

        Assert.NotNull(await store.FindByNameAsync("ORDERS.READ"));
    }

    [Fact]
    public async Task VerificationStore_FindActiveAsync_only_returns_pending_unexpired()
    {
        var store = new InMemoryVerificationStore();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await store.AddAsync(new Verification
        {
            Id = Guid.NewGuid(), TenantId = _tenantA, UserId = userId, Channel = VerificationChannel.Email,
            ExpiresAt = now.AddMinutes(10), Status = VerificationStatus.Pending, CreatedAt = now,
        });
        await store.AddAsync(new Verification
        {
            Id = Guid.NewGuid(), TenantId = _tenantA, UserId = userId, Channel = VerificationChannel.Email,
            ExpiresAt = now.AddMinutes(-1), Status = VerificationStatus.Pending, CreatedAt = now.AddMinutes(-30),
        });

        var active = await store.FindActiveAsync(_tenantA, userId, VerificationChannel.Email);

        Assert.NotNull(active);
        Assert.True(active.ExpiresAt > now);
    }

    private static InMemoryUserStore NewUserStore() =>
        new(new InMemoryRoleStore(new InMemoryPermissionStore()));

    private static InMemoryRoleStore NewRoleStore() =>
        new(new InMemoryPermissionStore());

    private static (InMemoryUserStore, InMemoryRoleStore) NewUserAndRoleStores()
    {
        var roles = new InMemoryRoleStore(new InMemoryPermissionStore());
        return (new InMemoryUserStore(roles), roles);
    }

    private async Task<Role> AddRole(IRoleStore store, string name)
    {
        var role = new Role { Id = Guid.NewGuid(), TenantId = _tenantA, Name = name };
        await store.AddAsync(role);
        return role;
    }
}
