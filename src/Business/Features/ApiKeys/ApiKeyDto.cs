using Domain.Entities.Identity;

namespace Business.Features.ApiKeys;

public sealed record ApiKeyDto(
    Guid Id,
    Guid UserId,
    string Name,
    string Prefix,
    string Last4,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    string? LastUsedIp,
    ApiKeyStatus Status,
    DateTime CreatedAt)
{
    public static ApiKeyDto From(ApiKey k) => new(
        k.Id, k.UserId, k.Name, k.Prefix, k.Last4,
        k.ExpiresAt, k.LastUsedAt, k.LastUsedIp, k.Status, k.CreatedAt);
}
