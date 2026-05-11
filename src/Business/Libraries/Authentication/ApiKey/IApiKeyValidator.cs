using System.Security.Claims;

namespace Business.Libraries.Authentication.ApiKey;

/// <summary>
/// Consumer-implemented contract. The handler reads the API key from the request header and asks
/// the validator whether the key is recognized. Implementers map the key to the principal's claim
/// set — typically a stable subject (the calling service's id) plus any role/permission claims
/// the authorization layer expects.
/// </summary>
public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}

public sealed record ApiKeyValidationResult(bool IsValid, IReadOnlyList<Claim> Claims)
{
    public static ApiKeyValidationResult Invalid { get; } = new(false, []);
    public static ApiKeyValidationResult Valid(IEnumerable<Claim> claims) => new(true, claims.ToList());
}
