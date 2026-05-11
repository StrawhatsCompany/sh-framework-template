using Business.Providers.Mail.Dtos;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Mails.Send;

public sealed class SendMailCommand : Request
{
    public required IReadOnlyList<MailAddress> To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? TextBody { get; init; }
    public IReadOnlyList<MailAddress>? Cc { get; init; }
    public IReadOnlyList<MailAddress>? Bcc { get; init; }
}
