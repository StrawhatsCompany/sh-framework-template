using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Login;

public sealed class LoginCommand : Request<LoginResponse>
{
    public Guid? TenantId { get; set; }
    public string? TenantSlug { get; set; }
    public string Identifier { get; set; } = "";   // email OR username
    public string Password { get; set; } = "";
}

public sealed class LoginResponse
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
}
