using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WebApi.Tests;

/// <summary>
/// Shared <see cref="WebApplicationFactory{TEntryPoint}"/> for integration tests. Injects a test
/// JWT signing key via <c>ConfigureAppConfiguration</c> so the framework's <c>ValidateOnStart</c>
/// guard passes — the production appsettings deliberately ships no SigningKey, and we mirror the
/// real wiring path (env vars / user-secrets) in tests rather than committing one to dev config.
/// </summary>
public sealed class SHWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "tests-only-signing-key-32-bytes-or-more-please-zzzzzzzzzzzzzzzzzzz";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SigningKey"] = TestSigningKey,
            });
        });
    }
}
