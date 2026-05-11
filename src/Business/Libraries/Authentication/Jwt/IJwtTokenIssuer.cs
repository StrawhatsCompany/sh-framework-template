using System.Security.Claims;

namespace Business.Libraries.Authentication.Jwt;

public interface IJwtTokenIssuer
{
    /// <summary>
    /// Mints a signed JWT for the given claim set. Populate role + permission claims here when
    /// the consumer's login handler issues the token; the authorization layer (issue #47) reads
    /// them on the receiving end.
    /// </summary>
    string Issue(IEnumerable<Claim> claims, TimeSpan? lifetime = null);
}
