using Business.Providers;
using Business.Providers.Mail;
using Business.Providers.Mail.Contracts;
using MimeKit;

namespace Providers.Mail.Smtp;

internal sealed class SmtpProvider(ISmtpClientPool pool, MailProviderCredential credential) : IMailProvider
{
    public async Task<ProviderResult<SendMailContract.Response>> SendAsync(SendMailContract.Request request, CancellationToken cancellationToken = default)
    {
        if (!request.MailBody.HasBody)
        {
            return ProviderResult.Failure<SendMailContract.Response>(MailProviderResultCode.NeedBody);
        }

        var message = BuildMessage(request);
        await pool.SendAsync(credential, message, cancellationToken);
        return ProviderResult.Success(new SendMailContract.Response(string.Empty, "SENT"));
    }

    private static MimeMessage BuildMessage(SendMailContract.Request request)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(request.From.FriendlyName, request.From.Address));

        AddRecipients(message.To, request.To);
        AddRecipients(message.Cc, request.Cc);
        AddRecipients(message.Bcc, request.Bcc);

        message.Subject = request.Subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = request.MailBody.TextBody,
            HtmlBody = request.MailBody.HtmlBody
        };

        if (request.MailBody.Attachments is { Count: > 0 } attachments)
        {
            foreach (var attachment in attachments)
            {
                bodyBuilder.Attachments.Add(
                    attachment.FileName,
                    attachment.File,
                    ContentType.Parse(attachment.ContentType.MediaType));
            }
        }

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private static void AddRecipients(InternetAddressList list, IEnumerable<Business.Providers.Mail.Dtos.MailAddress>? addresses)
    {
        if (addresses is null) return;
        foreach (var addr in addresses)
        {
            list.Add(new MailboxAddress(addr.FriendlyName, addr.Address));
        }
    }
}
