using Domain.Entities.Identity;
using Microsoft.Extensions.Hosting;

namespace Business.Identity;

/// <summary>
/// Idempotent seeder that ensures the standard <c>admin.*</c> permission rows exist in the
/// catalog on every startup. Concrete persistence-backed deployments can extend this with
/// app-specific permission seeds; the canonical admin set lives here so the framework's
/// admin endpoints have a permission catalog to gate against.
/// </summary>
internal sealed class PermissionSeeder(IPermissionStore permissions) : IHostedService
{
    private static readonly (string Name, string Description)[] AdminCatalog =
    [
        // Tenants
        ("admin.tenants.read",      "Read tenant records"),
        ("admin.tenants.write",     "Create, update, soft-delete tenants"),

        // Users
        ("admin.users.read",           "Read user records across the tenant"),
        ("admin.users.write",          "Create, update, soft-delete users"),
        ("admin.users.roles.write",    "Assign and unassign roles for any user"),
        ("admin.users.sessions.read",  "Read active sessions for any user"),
        ("admin.users.sessions.write", "Revoke active sessions for any user"),

        // Roles
        ("admin.roles.read",        "Read role records"),
        ("admin.roles.write",       "Create, update, soft-delete roles and their permission assignments"),

        // Permissions
        ("admin.permissions.read",  "Read permissions in the global catalog"),
        ("admin.permissions.write", "Create and delete permissions in the global catalog"),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var (name, description) in AdminCatalog)
        {
            if (await permissions.FindByNameAsync(name, cancellationToken) is not null) continue;

            var category = name.Split('.', 2)[0];
            await permissions.AddAsync(new Permission
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Category = category,
                CreatedAt = DateTime.UtcNow,
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
