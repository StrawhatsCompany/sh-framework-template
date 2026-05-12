using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class Verification
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasTenant, IHasStatus<VerificationStatus>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public VerificationChannel Channel { get; set; }
    public string CodeHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
