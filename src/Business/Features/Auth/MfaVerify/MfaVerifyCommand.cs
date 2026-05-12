using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.MfaVerify;

public sealed class MfaVerifyCommand : Request<MfaVerifyResponse>
{
    public Guid ChallengeId { get; set; }
    public string Code { get; set; } = "";
    public string? DeviceLabel { get; set; }
    public string? Ip { get; set; }
}

public sealed class MfaVerifyResponse
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
}
