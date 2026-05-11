namespace Business.Libraries.Authentication.Authorization;

/// <summary>
/// Consumer-owned source of truth for "which roles does this user have". Not shipped by the
/// framework — implement it backed by your user store (DB, identity provider, etc.) and register
/// in DI. The framework reads role claims off the principal during permission resolution; the
/// store is here for use cases that need to fetch roles outside of an active request (e.g. when
/// minting a JWT or refreshing claims).
/// </summary>
public interface IRoleStore
{
    Task<IReadOnlyCollection<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);
}
