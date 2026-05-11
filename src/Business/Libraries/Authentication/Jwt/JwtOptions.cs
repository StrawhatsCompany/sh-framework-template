namespace Business.Libraries.Authentication.Jwt;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 bytes (256 bits) when treated as UTF-8.
    /// Nullable to permit binding the rest of the options when JWT isn't wired; when JWT bearer is
    /// active, <see cref="JwtOptionsValidator"/> rejects null/short keys at startup. Never put
    /// this in <c>appsettings.json</c> — read from user-secrets (dev) or env vars / a secret
    /// store (prod). See <c>docs/SECRETS.md</c>.
    /// </summary>
    public string? SigningKey { get; init; }

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromSeconds(30);
}
