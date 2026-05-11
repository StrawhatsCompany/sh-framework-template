using Business.Providers.Mail.Dtos;

namespace Business.Providers.Mail.Contracts;

public class SendMailContract
{
    public record Request(MailAddress From, List<MailAddress> To, string Subject, MailBody MailBody, List<MailAddress>? Cc = null, List<MailAddress>? Bcc = null);
    public record Response(string ProviderRecordId, string Status);
}
