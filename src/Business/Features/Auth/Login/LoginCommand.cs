using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Login;

public sealed class LoginCommand : Request<LoginResponse>
{
    public Guid? TenantId { get; set; }
    public string? TenantSlug { get; set; }
    public string Identifier { get; set; } = "";   // email OR username
    public string Password { get; set; } = "";
    public string? DeviceLabel { get; set; }       // optional client hint (e.g. "Chrome on Windows")
    public string? Ip { get; set; }                // populated by the endpoint from HttpContext
}

/// <summary>
/// Polymorphic response. When the user has any Active MFA factor enrolled, the password check
/// passes but the response surfaces <c>MfaRequired = true</c> with a challenge id + kind;
/// callers complete the login via <c>POST /api/v1/auth/mfa/verify</c>. Otherwise the full
/// access + refresh pair is returned in one round trip.
/// </summary>
public sealed class LoginResponse
{
    public bool MfaRequired { get; init; }

    // Set when MfaRequired is true.
    public Guid? ChallengeId { get; init; }
    public MfaFactorKind? ChallengeKind { get; init; }
    public DateTime? ChallengeExpiresAt { get; init; }

    // Set when MfaRequired is false (no MFA enrolled).
    public string? AccessToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? RefreshTokenExpiresAt { get; init; }
    public Guid? SessionId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? TenantId { get; init; }
}
