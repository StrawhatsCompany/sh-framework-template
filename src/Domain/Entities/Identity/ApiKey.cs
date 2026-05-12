using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class ApiKey
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasAuditColumns, ISoftDeletable,
      IHasTenant, IHasStatus<ApiKeyStatus>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";            // admin-visible label
    public string Prefix { get; set; } = "";          // 8 random alphanumerics; shown in admin UI
    public string Last4 { get; set; } = "";           // last 4 chars of the secret
    public string KeyHash { get; set; } = "";         // SHA-256 of the FULL token "shf_<prefix>_<secret>"
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public ApiKeyStatus Status { get; set; } = ApiKeyStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
