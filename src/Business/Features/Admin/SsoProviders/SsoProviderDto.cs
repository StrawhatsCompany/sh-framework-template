using Domain.Entities.Identity;

namespace Business.Features.Admin.SsoProviders;

public sealed record SsoProviderDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string DisplayName,
    SsoProtocol Protocol,
    string? DiscoveryUrl,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string? UserInfoEndpoint,
    string? JwksUri,
    string Issuer,
    string ClientId,
    /// <summary>Always masked — full secret is never returned.</summary>
    string ClientSecretMasked,
    string Scopes,
    string ClaimMappingJson,
    SsoProviderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static SsoProviderDto From(SsoProvider p) => new(
        p.Id, p.TenantId, p.Name, p.DisplayName, p.Protocol,
        p.DiscoveryUrl, p.AuthorizationEndpoint, p.TokenEndpoint, p.UserInfoEndpoint,
        p.JwksUri, p.Issuer, p.ClientId, "****",
        p.Scopes, p.ClaimMappingJson, p.Status, p.CreatedAt, p.UpdatedAt);
}
