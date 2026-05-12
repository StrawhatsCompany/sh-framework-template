using Domain.Entities.Identity;

namespace Business.Features.Admin.Permissions;

public sealed record PermissionDto(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PermissionDto From(Permission p) => new(p.Id, p.Name, p.Description, p.Category, p.CreatedAt, p.UpdatedAt);
}
