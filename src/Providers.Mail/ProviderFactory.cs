using Business.Providers;
using Business.Providers.Mail;
using Providers.Mail.Smtp;

namespace Providers.Mail;

internal sealed class ProviderFactory(ISmtpClientPool smtpPool) : IProviderFactory<MailProviderCredential, IMailProvider>
{
    public IMailProvider Create(MailProviderCredential credential) =>
        credential.ProviderType switch
        {
            MailProviderType.Smtp => new SmtpProvider(smtpPool, credential),
            _ => throw new NotSupportedException($"{credential.ProviderType} is not supported from Providers.Mail")
        };
}
