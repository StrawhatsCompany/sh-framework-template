using Business.Providers;
using Business.Providers.Mail;
using Business.Providers.Mail.Contracts;
using Business.Providers.Mail.Dtos;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Mails.Send;

public sealed class SendMailCommand: Request
{
}

public sealed class SendMailHandler(IProviderFactory<MailProviderCredential, IMailProvider> mailProviderFactory) : RequestHandler<SendMailCommand>
{
    public override async Task<Result> HandleAsync(SendMailCommand request, CancellationToken cancellationToken = default)
    {
        var mailProvider = mailProviderFactory.Create(new MailProviderCredential { 
            HostName = "localhost",
            Port = 1025,
            UseSsl = false,
        });

        var mail = new SendMailContract.Request(
            new MailAddress("noreply@example.com", "Sender"),
            new List<MailAddress>() {
                new MailAddress("recipient@example.com", "Recipient")
            },
            "Test Subject",
            MailBody.Instance().WithHtmlBody("<h1>Hello world!</h1>")
        );

        var mailResult = await mailProvider.SendAsync(mail, cancellationToken);

        if (mailResult.IsSuccess)
        {
            return Result.Success();
        }

        var result = Result.Failure(ResultCode.Failure, mailResult.Errors.ToDictionary());

        return result;
    }
}
