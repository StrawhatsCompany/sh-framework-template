using Domain.Entities.Identity;

namespace Business.Features.Admin.Users;

public sealed record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    string Username,
    string? Phone,
    string DisplayName,
    DateTime? EmailVerifiedAt,
    DateTime? PhoneVerifiedAt,
    UserStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static UserDto From(User u) => new(
        u.Id, u.TenantId, u.Email, u.Username, u.Phone, u.DisplayName,
        u.EmailVerifiedAt, u.PhoneVerifiedAt, u.Status, u.CreatedAt, u.UpdatedAt);
}
