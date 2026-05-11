using Business.Providers;
using Business.Providers.Mail;
using Business.Providers.Mail.Contracts;
using Business.Providers.Mail.Dtos;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Mails.Send;

public sealed class SendMailHandler(
    IProviderFactory<MailProviderCredential, IMailProvider> mailProviderFactory,
    IOptions<MailOptions> mailOptions)
    : RequestHandler<SendMailCommand>
{
    public override async Task<Result> HandleAsync(SendMailCommand request, CancellationToken cancellationToken = default)
    {
        var options = mailOptions.Value;

        var provider = mailProviderFactory.Create(new MailProviderCredential
        {
            ProviderType = MailProviderType.Smtp,
            HostName = options.HostName,
            Port = options.Port,
            UseSsl = options.UseSsl,
        });

        var body = MailBody.Instance().WithHtmlBody(request.HtmlBody);
        if (!string.IsNullOrEmpty(request.TextBody))
        {
            body.WithTextBody(request.TextBody);
        }

        var contract = new SendMailContract.Request(
            new MailAddress(options.FromAddress, options.FromName),
            request.To.ToList(),
            request.Subject,
            body,
            request.Cc?.ToList(),
            request.Bcc?.ToList());

        var providerResult = await provider.SendAsync(contract, cancellationToken);

        return providerResult.IsSuccess
            ? Result.Success()
            : Result.Failure(ResultCode.Failure, providerResult.Errors.ToDictionary());
    }
}
