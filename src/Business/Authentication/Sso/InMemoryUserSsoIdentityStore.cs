using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Authentication.Sso;

internal sealed class InMemoryUserSsoIdentityStore : IUserSsoIdentityStore
{
    private readonly ConcurrentDictionary<Guid, UserSsoIdentity> _byId = new();

    public Task<UserSsoIdentity> AddAsync(UserSsoIdentity identity, CancellationToken ct = default)
    {
        _byId[identity.Id] = identity;
        return Task.FromResult(identity);
    }

    public Task<UserSsoIdentity?> FindByProviderSubjectAsync(
        Guid tenantId, Guid ssoProviderId, string externalSubject, CancellationToken ct = default)
    {
        var match = _byId.Values.FirstOrDefault(i =>
            i.TenantId == tenantId && i.SsoProviderId == ssoProviderId &&
            string.Equals(i.ExternalSubject, externalSubject, StringComparison.Ordinal));
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<UserSsoIdentity>> ListByUserAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<UserSsoIdentity> snapshot = _byId.Values
            .Where(i => i.TenantId == tenantId && i.UserId == userId)
            .ToList();
        return Task.FromResult(snapshot);
    }
}
