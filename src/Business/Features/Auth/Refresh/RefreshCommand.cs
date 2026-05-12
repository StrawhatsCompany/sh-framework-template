using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Refresh;

public sealed class RefreshCommand : Request<RefreshResponse>
{
    public string RefreshToken { get; set; } = "";
    public string? Ip { get; set; }
}

public sealed class RefreshResponse
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
    public required Guid SessionId { get; init; }
}
