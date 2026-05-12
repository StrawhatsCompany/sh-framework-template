using Domain.Entities.Identity;

namespace Business.Authentication.Sso;

public interface IUserSsoIdentityStore
{
    Task<UserSsoIdentity> AddAsync(UserSsoIdentity identity, CancellationToken ct = default);
    Task<UserSsoIdentity?> FindByProviderSubjectAsync(
        Guid tenantId, Guid ssoProviderId, string externalSubject, CancellationToken ct = default);
    Task<IReadOnlyList<UserSsoIdentity>> ListByUserAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default);
}
