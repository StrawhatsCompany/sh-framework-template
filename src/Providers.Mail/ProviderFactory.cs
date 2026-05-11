using Business.Providers;
using Business.Providers.Mail;
using Providers.Mail.Smtp;

namespace Providers.Mail;

internal class ProviderFactory: IProviderFactory<MailProviderCredential, IMailProvider>
{
    public IMailProvider Create(MailProviderCredential credential)
    {
        return credential.ProviderType switch { 
            MailProviderType.Smtp => new SmtpProvider(credential),
            _ => throw new NotSupportedException($"{credential.ProviderType.ToString()} is not supported from Providers.Mail")
        };
    }
}
