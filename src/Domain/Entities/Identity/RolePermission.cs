using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class RolePermission
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasAuditColumns, IHasTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
