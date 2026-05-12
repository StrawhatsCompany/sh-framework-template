using Domain.Abstractions;

namespace Domain.Entities.Identity;

public sealed class SsoProvider
    : IPrimaryKey<Guid>, IHasCreatedColumns, IHasAuditColumns, ISoftDeletable,
      IHasTenant, IHasStatus<SsoProviderStatus>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";                       // unique per tenant, URL-safe (e.g. "google")
    public string DisplayName { get; set; } = "";                // shown on login page
    public SsoProtocol Protocol { get; set; }

    // Endpoints — OIDC discovery doc populates these on POST/PATCH; admins can also set
    // them manually (for non-conforming providers or when discovery URL is unavailable).
    public string? DiscoveryUrl { get; set; }
    public string AuthorizationEndpoint { get; set; } = "";
    public string TokenEndpoint { get; set; } = "";
    public string? UserInfoEndpoint { get; set; }
    public string? JwksUri { get; set; }                         // OIDC: id_token signature verification
    public string Issuer { get; set; } = "";

    public string ClientId { get; set; } = "";
    public string ClientSecretCipher { get; set; } = "";          // encrypted via ICredentialProtector

    public string Scopes { get; set; } = "openid profile email";

    /// <summary>
    /// JSON mapping from local fields to IdP claim names, e.g.
    /// <c>{ "email": "email", "username": "preferred_username", "displayName": "name" }</c>.
    /// Used during callback to project the IdP's claim set into our User fields.
    /// </summary>
    public string ClaimMappingJson { get; set; } = "{}";

    public SsoProviderStatus Status { get; set; } = SsoProviderStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
