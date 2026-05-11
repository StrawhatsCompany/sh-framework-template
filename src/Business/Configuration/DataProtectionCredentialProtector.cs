using Microsoft.AspNetCore.DataProtection;

namespace Business.Configuration;

internal sealed class DataProtectionCredentialProtector : ICredentialProtector
{
    private const string Purpose = "Business.Configuration.ServiceReference.Credentials";
    private readonly IDataProtector _protector;

    public DataProtectionCredentialProtector(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector(Purpose);

    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
