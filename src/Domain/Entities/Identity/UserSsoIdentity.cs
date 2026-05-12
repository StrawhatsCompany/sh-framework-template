using Domain.Abstractions;

namespace Domain.Entities.Identity;

/// <summary>
/// Links a local user to an external identity issued by an SSO provider. The pair
/// (SsoProviderId, ExternalSubject) is unique — that's how subsequent logins match without
/// touching the email (the IdP's "sub" is the canonical identifier).
/// </summary>
public sealed class UserSsoIdentity
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasAuditColumns, IHasTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid SsoProviderId { get; set; }
    public string ExternalSubject { get; set; } = "";    // the IdP's "sub" claim
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
