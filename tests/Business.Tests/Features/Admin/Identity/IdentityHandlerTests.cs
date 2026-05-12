using Business.Common;
using Business.Features.Admin.Permissions.CreatePermission;
using Business.Features.Admin.Roles.CreateRole;
using Business.Features.Admin.Roles.DeleteRole;
using Business.Features.Admin.Roles.SetRolePermissions;
using Business.Features.Admin.Users.CreateUser;
using Business.Features.Admin.Users.ListUsers;
using Business.Features.Admin.Users.SetUserRoles;
using Business.Identity;
using Domain.Entities.Identity;

namespace Business.Tests.Features.Admin.Identity;

public class IdentityHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task CreateUser_hashes_password_and_stamps_audit()
    {
        var (users, _, _, hasher, tenantCtx, userCtx) = BuildScope(actingUser: Guid.NewGuid());
        var handler = new CreateUserHandler(users, hasher, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new CreateUserCommand
        {
            Email = "User@Example.COM",
            Username = "alice",
            Password = "correct horse battery staple",
            DisplayName = "Alice",
        });

        Assert.True(result.IsSuccess);
        var created = result.Data!.User;
        Assert.Equal("user@example.com", created.Email);
        var stored = await users.FindByIdAsync(_tenantId, created.Id);
        Assert.NotNull(stored);
        Assert.NotNull(stored.PasswordHash);
        Assert.True(hasher.Verify("correct horse battery staple", stored.PasswordHash));
    }

    [Fact]
    public async Task CreateUser_rejects_duplicate_email_in_same_tenant()
    {
        var (users, _, _, hasher, tenantCtx, userCtx) = BuildScope();
        await users.AddAsync(new User
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, Email = "taken@x.com", Username = "first",
        });
        var handler = new CreateUserHandler(users, hasher, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new CreateUserCommand
        {
            Email = "TAKEN@x.com", Username = "second", DisplayName = "x",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.UserEmailAlreadyExists.Code, result.Code);
    }

    [Fact]
    public async Task CreateUser_rejects_weak_password()
    {
        var (users, _, _, hasher, tenantCtx, userCtx) = BuildScope();
        var handler = new CreateUserHandler(users, hasher, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new CreateUserCommand
        {
            Email = "u@x.com", Username = "alice", Password = "short", DisplayName = "x",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.UserPasswordTooWeak.Code, result.Code);
    }

    [Fact]
    public async Task CreateUser_returns_TenantRequired_when_no_tenant_context()
    {
        var (users, _, _, hasher, _, userCtx) = BuildScope();
        var nullTenant = Substitute.For<ITenantContext>();
        nullTenant.TenantId.Returns((Guid?)null);
        var handler = new CreateUserHandler(users, hasher, nullTenant, userCtx);

        var result = await handler.HandleAsync(new CreateUserCommand
        {
            Email = "u@x.com", Username = "u", DisplayName = "x",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.TenantRequired.Code, result.Code);
    }

    [Fact]
    public async Task ListUsers_filters_by_status()
    {
        var (users, _, _, _, tenantCtx, _) = BuildScope();
        await users.AddAsync(new User { Id = Guid.NewGuid(), TenantId = _tenantId, Email = "a@x", Username = "a", Status = UserStatus.Active });
        await users.AddAsync(new User { Id = Guid.NewGuid(), TenantId = _tenantId, Email = "b@x", Username = "b", Status = UserStatus.Disabled });
        var handler = new ListUsersHandler(users, tenantCtx);

        var result = await handler.HandleAsync(new ListUsersQuery { Status = UserStatus.Active });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
        Assert.Equal(UserStatus.Active, result.Data.Items[0].Status);
    }

    [Fact]
    public async Task CreateRole_rejects_duplicate_name()
    {
        var (_, roles, _, _, tenantCtx, userCtx) = BuildScope();
        await roles.AddAsync(new Role { Id = Guid.NewGuid(), TenantId = _tenantId, Name = "Admin" });
        var handler = new CreateRoleHandler(roles, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new CreateRoleCommand { Name = "Admin" });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.RoleNameAlreadyExists.Code, result.Code);
    }

    [Fact]
    public async Task DeleteRole_refuses_system_roles()
    {
        var (_, roles, _, _, tenantCtx, userCtx) = BuildScope();
        var sys = new Role { Id = Guid.NewGuid(), TenantId = _tenantId, Name = "System", IsSystem = true };
        await roles.AddAsync(sys);
        var handler = new DeleteRoleHandler(roles, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new DeleteRoleCommand { Id = sys.Id });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.RoleSystemImmutable.Code, result.Code);
    }

    [Fact]
    public async Task SetUserRoles_returns_RoleNotFound_for_unknown_role()
    {
        var (users, roles, _, _, tenantCtx, userCtx) = BuildScope();
        var userId = Guid.NewGuid();
        await users.AddAsync(new User { Id = userId, TenantId = _tenantId, Email = "u@x", Username = "u" });
        var handler = new SetUserRolesHandler(users, roles, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new SetUserRolesCommand
        {
            UserId = userId,
            RoleIds = [Guid.NewGuid()],
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.RoleNotFound.Code, result.Code);
    }

    [Fact]
    public async Task SetRolePermissions_assigns_known_permissions()
    {
        var (_, roles, permissions, _, tenantCtx, userCtx) = BuildScope();
        var role = new Role { Id = Guid.NewGuid(), TenantId = _tenantId, Name = "Editor" };
        await roles.AddAsync(role);
        var perm = new Permission { Id = Guid.NewGuid(), Name = "orders.write", Category = "orders" };
        await permissions.AddAsync(perm);
        var handler = new SetRolePermissionsHandler(roles, permissions, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new SetRolePermissionsCommand
        {
            RoleId = role.Id,
            PermissionIds = [perm.Id],
        });

        Assert.True(result.IsSuccess);
        var assigned = await roles.ListPermissionsAsync(_tenantId, role.Id);
        Assert.Single(assigned);
        Assert.Equal("orders.write", assigned[0].Name);
    }

    [Fact]
    public async Task CreatePermission_validates_name_format()
    {
        var permissions = new InMemoryPermissionStore();
        var handler = new CreatePermissionHandler(permissions, Substitute.For<IUserContext>());

        var bad = await handler.HandleAsync(new CreatePermissionCommand { Name = "InvalidName" });
        Assert.False(bad.IsSuccess);
        Assert.Equal(IdentityResultCode.PermissionNameInvalid.Code, bad.Code);

        var good = await handler.HandleAsync(new CreatePermissionCommand { Name = "orders.read", Description = "Read orders" });
        Assert.True(good.IsSuccess);
        Assert.Equal("orders", good.Data!.Permission.Category);
    }

    private (
        InMemoryUserStore users,
        InMemoryRoleStore roles,
        InMemoryPermissionStore permissions,
        Argon2idPasswordHasher hasher,
        ITenantContext tenantCtx,
        IUserContext userCtx) BuildScope(Guid? actingUser = null)
    {
        var permissions = new InMemoryPermissionStore();
        var roles = new InMemoryRoleStore(permissions);
        var users = new InMemoryUserStore(roles);
        var hasher = new Argon2idPasswordHasher();
        var tenantCtx = Substitute.For<ITenantContext>();
        tenantCtx.TenantId.Returns(_tenantId);
        var userCtx = Substitute.For<IUserContext>();
        userCtx.UserId.Returns(actingUser);
        return (users, roles, permissions, hasher, tenantCtx, userCtx);
    }
}
