using Business.Providers.Mail;
using MimeKit;

namespace Providers.Mail.Smtp;

internal interface ISmtpClientPool
{
    Task SendAsync(MailProviderCredential credential, MimeMessage message, CancellationToken cancellationToken = default);
}
