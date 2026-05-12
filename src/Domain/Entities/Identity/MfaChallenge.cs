using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class MfaChallenge
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasTenant, IHasStatus<MfaChallengeStatus>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid MfaFactorId { get; set; }
    public MfaFactorKind Kind { get; set; }                // denormalised for response payloads
    public string? CodeHash { get; set; }                  // SHA-256 for Email/SMS; null for TOTP (recomputed)
    public DateTime ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public MfaChallengeStatus Status { get; set; } = MfaChallengeStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
