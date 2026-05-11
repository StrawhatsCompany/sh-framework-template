using Business.Providers;
using Business.Providers.Mail;
using Business.Providers.Mail.Contracts;
using MailKit.Net.Smtp;
using MimeKit;

namespace Providers.Mail.Smtp;

public class SmtpProvider(MailProviderCredential credential) : IMailProvider
{
    public async Task<ProviderResult<SendMailContract.Response>> SendAsync(SendMailContract.Request request, CancellationToken cancellationToken = default)
    {
        if (!request.MailBody.HasBody)
        {
            return ProviderResult.Failure<SendMailContract.Response>(MailProviderResultCode.NeedBody);
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(request.From.FriendlyName, request.From.Address));

        if (request.To != null)
        {
            foreach (var to in request.To)
            {
                message.To.Add(new MailboxAddress(to.FriendlyName, to.Address));
            }
        }

        if (request.Cc != null)
        {
            foreach (var cc in request.Cc)
            {
                message.Cc.Add(new MailboxAddress(cc.FriendlyName, cc.Address));
            }
        }

        if (request.Bcc != null)
        {
            foreach (var bcc in request.Bcc)
            {
                message.Bcc.Add(new MailboxAddress(bcc.FriendlyName, bcc.Address));
            }
        }

        message.Subject = request.Subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = request.MailBody.TextBody,
            HtmlBody = request.MailBody.HtmlBody
        };

        if (request.MailBody.Attachments != null)
        {
            foreach (var attachment in request.MailBody.Attachments)
            {
                await bodyBuilder.Attachments.AddAsync(attachment.FileName, attachment.File, new ContentType(attachment.ContentType.MediaType, attachment.ContentType.Name), cancellationToken);
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(credential.HostName, credential.Port, credential.UseSsl, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        return ProviderResult.Success(new SendMailContract.Response(string.Empty, "SENT"));
    }
}
