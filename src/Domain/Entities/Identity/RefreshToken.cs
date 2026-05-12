using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class RefreshToken
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasTenant, IHasStatus<RefreshTokenStatus>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SessionId { get; set; }
    public string TokenHash { get; set; } = "";       // SHA-256 of the plaintext token
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }          // set when rotated
    public Guid? ReplacedById { get; set; }            // forms the rotation chain for family invalidation
    public RefreshTokenStatus Status { get; set; } = RefreshTokenStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
