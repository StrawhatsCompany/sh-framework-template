using Microsoft.AspNetCore.Authentication;

namespace Business.Authentication.ApiKeys;

public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    public const string SectionName = "Authentication:ApiKey";

    /// <summary>
    /// Minimum gap between LastUsedAt persistence writes per key. Keeps the auth hot-path
    /// from doing a DB write on every request when a key is used heavily.
    /// </summary>
    public TimeSpan LastUsedUpdateThrottle { get; set; } = TimeSpan.FromSeconds(60);
}
