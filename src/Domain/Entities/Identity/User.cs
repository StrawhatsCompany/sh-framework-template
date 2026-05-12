using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class User
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasAuditColumns, ISoftDeletable,
      IHasTenant, IHasStatus<UserStatus>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public string DisplayName { get; set; } = "";
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
