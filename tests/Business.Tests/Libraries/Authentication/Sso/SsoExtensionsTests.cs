using Business.Libraries.Authentication;
using Business.Libraries.Authentication.Sso;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Tests.Libraries.Authentication.Sso;

public class SsoExtensionsTests
{
    [Fact]
    public void AddSso_calls_Configure_on_every_provider()
    {
        var google = new RecordingProvider("Google");
        var entra = new RecordingProvider("Entra");
        var builder = NewBuilder();

        builder.AddSso(google, entra);

        Assert.True(google.Configured);
        Assert.True(entra.Configured);
    }

    [Fact]
    public void AddSso_registers_each_provider_in_DI()
    {
        var google = new RecordingProvider("Google");
        var builder = NewBuilder();

        builder.AddSso(google);

        var registered = builder.Services
            .Where(d => d.ServiceType == typeof(ISsoProvider))
            .Select(d => d.ImplementationInstance)
            .OfType<ISsoProvider>()
            .ToList();
        Assert.Contains(google, registered);
    }

    [Fact]
    public void AddSso_throws_on_duplicate_schemes()
    {
        var a = new RecordingProvider("Google");
        var b = new RecordingProvider("Google");

        Assert.Throws<InvalidOperationException>(() => NewBuilder().AddSso(a, b));
    }

    [Fact]
    public void AddSso_rejects_null_provider()
    {
        Assert.Throws<ArgumentNullException>(() => NewBuilder().AddSso(null!));
    }

    private static SHAuthenticationBuilder NewBuilder()
    {
        var services = new ServiceCollection();
        var auth = services.AddAuthentication();
        var configuration = new ConfigurationBuilder().Build();
        return new SHAuthenticationBuilder(services, configuration, auth);
    }

    private sealed class RecordingProvider(string scheme) : ISsoProvider
    {
        public string Scheme { get; } = scheme;
        public bool Configured { get; private set; }

        public void Configure(AuthenticationBuilder builder, IConfiguration configuration)
        {
            Configured = true;
        }
    }
}
