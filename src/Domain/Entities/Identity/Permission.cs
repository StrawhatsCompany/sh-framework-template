using Domain.Abstractions;

namespace Domain.Entities.Identity;

// Permissions are global — no IHasTenant. Tenants share the catalog; each tenant's roles
// pick which permissions they grant.
public sealed class Permission
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasAuditColumns
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";       // dotted lowercase: admin.users.write, orders.read
    public string? Description { get; set; }
    public string Category { get; set; } = "";   // first segment, denormalised for admin UI grouping
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
