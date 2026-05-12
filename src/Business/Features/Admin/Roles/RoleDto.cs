using Domain.Entities.Identity;

namespace Business.Features.Admin.Roles;

public sealed record RoleDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    bool IsSystem,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static RoleDto From(Role r) => new(r.Id, r.TenantId, r.Name, r.Description, r.IsSystem, r.CreatedAt, r.UpdatedAt);
}
