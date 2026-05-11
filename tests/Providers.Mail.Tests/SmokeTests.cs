using Providers.Mail;

namespace Providers.Mail.Tests;

public class SmokeTests
{
    [Fact]
    public void Mail_provider_assembly_loads()
    {
        var assembly = typeof(MailProviderResultCode).Assembly;
        Assert.NotNull(assembly);
    }
}
