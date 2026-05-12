using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class MfaFactor
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasAuditColumns, ISoftDeletable,
      IHasTenant, IHasStatus<MfaFactorStatus>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public MfaFactorKind Kind { get; set; }
    public string? SecretCipher { get; set; }       // TOTP shared secret, encrypted via ICredentialProtector
    public string? Destination { get; set; }        // email/phone — denormalised from User for channel routing
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public MfaFactorStatus Status { get; set; } = MfaFactorStatus.PendingEnrollment;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
