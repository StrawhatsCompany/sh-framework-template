using Domain.Entities.Identity;

namespace Business.Features.Auth.Sessions;

public sealed record SessionDto(
    Guid Id,
    Guid UserId,
    SessionAuthMethod AuthMethod,
    string? DeviceLabel,
    string? IpFirst,
    string? IpLast,
    DateTime LastSeenAt,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    SessionStatus Status)
{
    public static SessionDto From(Session s) => new(
        s.Id, s.UserId, s.AuthMethod, s.DeviceLabel, s.IpFirst, s.IpLast,
        s.LastSeenAt, s.ExpiresAt, s.CreatedAt, s.Status);
}
