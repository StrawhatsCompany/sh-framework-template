using Domain.Entities.Identity;

namespace Business.Authentication.Jwt;

public interface IJwtTokenIssuer
{
    /// <summary>
    /// Mints an access token for the given user. Claims emitted: <c>sub</c> (user id),
    /// <c>tid</c> (tenant id), <c>email</c>, <c>preferred_username</c>, <c>name</c>, and
    /// <c>sid</c> (session id) when supplied. No <c>permissions</c> claim — authorization
    /// resolves against the DB catalog at request time so role changes apply immediately
    /// without waiting for token expiry.
    /// </summary>
    IssuedToken Issue(User user, Guid? sessionId = null);
}

public sealed record IssuedToken(string AccessToken, DateTime ExpiresAt);
