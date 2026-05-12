using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class Session
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasAuditColumns, IHasTenant, IHasStatus<SessionStatus>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public SessionAuthMethod AuthMethod { get; set; }
    public string? DeviceLabel { get; set; }
    public string? IpFirst { get; set; }
    public string? IpLast { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
