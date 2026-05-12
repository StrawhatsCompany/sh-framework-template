using Domain.Entities.Identity;

namespace Business.Features.Admin.Tenants;

public sealed record TenantDto(
    Guid Id,
    string Slug,
    string DisplayName,
    TenantStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static TenantDto From(Tenant t) => new(t.Id, t.Slug, t.DisplayName, t.Status, t.CreatedAt, t.UpdatedAt);
}
