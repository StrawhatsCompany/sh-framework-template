using Business.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Tests.Configuration;

public class DataProtectionCredentialProtectorTests
{
    [Fact]
    public void Round_trip_recovers_the_plaintext()
    {
        var protector = NewProtector();
        const string plaintext = "{\"username\":\"smtp\",\"password\":\"shh\"}";

        var cipher = protector.Protect(plaintext);
        var recovered = protector.Unprotect(cipher);

        Assert.Equal(plaintext, recovered);
        Assert.NotEqual(plaintext, cipher);
    }

    [Fact]
    public void Cipher_differs_from_plaintext()
    {
        var protector = NewProtector();

        var cipher = protector.Protect("secret");

        Assert.NotEqual("secret", cipher);
    }

    private static DataProtectionCredentialProtector NewProtector()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        return new DataProtectionCredentialProtector(provider);
    }
}
