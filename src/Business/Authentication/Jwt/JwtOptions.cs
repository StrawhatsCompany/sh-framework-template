namespace Business.Authentication.Jwt;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = "shframework";
    public string Audience { get; set; } = "shframework";
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 UTF-8 bytes (256 bits). Sourced from
    /// user-secrets / env (never appsettings or the DB). Required in production; dev can
    /// auto-generate, but the framework warns loudly when that path runs outside Development.
    /// </summary>
    public string? SigningKey { get; set; }
}
