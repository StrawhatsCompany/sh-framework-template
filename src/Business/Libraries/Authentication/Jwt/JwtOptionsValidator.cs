using System.Text;
using Microsoft.Extensions.Options;

namespace Business.Libraries.Authentication.Jwt;

/// <summary>
/// Validates <see cref="JwtOptions"/> when JWT bearer is wired. Fails fast at startup if the
/// signing key is missing or too short (HMAC-SHA256 needs ≥ 256 bits) instead of letting the
/// service boot with a nonsense key and reject every token at runtime.
/// </summary>
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Authentication:Jwt:Issuer is required.");
        }
        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Authentication:Jwt:Audience is required.");
        }
        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add("Authentication:Jwt:SigningKey is required. Set via `dotnet user-secrets set` (dev) or env var (prod). See docs/SECRETS.md.");
        }
        else if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            failures.Add("Authentication:Jwt:SigningKey must be at least 32 UTF-8 bytes (256 bits) for HMAC-SHA256.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
